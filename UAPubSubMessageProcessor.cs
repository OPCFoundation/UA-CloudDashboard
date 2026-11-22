
namespace Opc.Ua.Cloud.Dashboard
{
    using Microsoft.AspNetCore.SignalR;
    using Opc.Ua;
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Encoding.Json;
    using Opc.Ua.PubSub.Encoding.Uadp;
    using Opc.Ua.PubSub.MetaData;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using UACloudDashboard.Interfaces;
    using JsonDataSetMessage = Opc.Ua.PubSub.Encoding.Json.JsonDataSetMessage;
    using JsonDecoder = Opc.Ua.PubSub.Encoding.Json.JsonDecoder;
    using JsonNetworkMessage = Opc.Ua.PubSub.Encoding.Json.JsonNetworkMessage;

    public class UAPubSubMessageProcessor : IMessageProcessor
    {
        private StatusHubClient _hubClient;
        private readonly Dictionary<string, DataSetMetaDataType> _metaData = new();
        private readonly IDataSetMetaDataRegistry _metaDataRegistry = new WriterGroupFallbackMetaDataRegistry(new DataSetMetaDataRegistry());
        private readonly PubSubNetworkMessageContext _networkMessageContext;
        private readonly JsonDecoder _jsonDecoder = new JsonDecoder();

        public UAPubSubMessageProcessor(IHubContext<StatusHub> hubContext)
        {
            _hubClient = new StatusHubClient(hubContext);

            _networkMessageContext = new PubSubNetworkMessageContext(
                ServiceMessageContext.Create(telemetry: null),
                _metaDataRegistry,
                new PubSubDiagnostics(PubSubDiagnosticsLevel.Low, TimeProvider.System),
                TimeProvider.System,
                PubSubFieldEncoding.Variant);
        }

        public void Clear()
        {
            lock (_hubClient.TableEntries)
            {
                _hubClient.TableEntries.Clear();
            }
        }

        public void ProcessMessage(byte[] payload, DateTime receivedTime, string contentType)
        {
            string message = string.Empty;
            try
            {
                message = Encoding.UTF8.GetString(payload);
                if (message != null)
                {
                    if (((contentType != null) && (contentType == "application/json")) || message.TrimStart().StartsWith('{') || message.TrimStart().StartsWith('['))
                    {
                        if (message.TrimStart().StartsWith('['))
                        {
                            // we received an array of messages
                            using JsonDocument messageArray = JsonDocument.Parse(payload);
                            foreach (JsonElement singleMessage in messageArray.RootElement.EnumerateArray())
                            {
                                string singleMessageText = singleMessage.GetRawText();
                                DecodeMessage(_jsonDecoder.TryDecodeAsync(Encoding.UTF8.GetBytes(singleMessageText), _networkMessageContext).GetAwaiter().GetResult(), receivedTime, singleMessageText);
                            }
                        }
                        else
                        {
                            DecodeMessage(_jsonDecoder.TryDecodeAsync(payload, _networkMessageContext).GetAwaiter().GetResult(), receivedTime, message);
                        }
                    }
                    else
                    {
                        DecodeMessage(UadpDecoder.Decode(payload, _networkMessageContext), receivedTime, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception {ex.Message} processing message {message}");
            }
        }

        private void AddMetaData(PublisherId publisherId, ushort writerGroupId, ushort dataSetWriterId, Uuid dataSetClassId, DataSetMetaDataType metadata)
        {
            if (metadata == null)
            {
                return;
            }

            DataSetMetaDataKey key = new DataSetMetaDataKey(publisherId, writerGroupId, dataSetWriterId, dataSetClassId, metadata.ConfigurationVersion?.MajorVersion ?? 0);
            _metaDataRegistry.Register(in key, metadata);

            _metaData[publisherId.ToString() + ":" + dataSetWriterId.ToString()] = metadata;
        }

        private void DecodeMessage(PubSubNetworkMessage encodedMessage, DateTime receivedTime, string message)
        {
            if (encodedMessage == null)
            {
                Trace.TraceError($"Could not decode message {message}");
                return;
            }

            if (encodedMessage is JsonMetaDataMessage jsonMetaDataMessage)
            {
                // setup metadata for subsequent data messages
                AddMetaData(jsonMetaDataMessage.PublisherId, 0, jsonMetaDataMessage.DataSetWriterId, jsonMetaDataMessage.DataSetClassId, jsonMetaDataMessage.MetaDataPayload);
            }
            else if (encodedMessage is UadpDiscoveryResponseMessage uadpDiscoveryResponse)
            {
                if (uadpDiscoveryResponse.DiscoveryType == UadpDiscoveryType.DataSetMetaData)
                {
                    // setup metadata for subsequent data messages
                    AddMetaData(uadpDiscoveryResponse.PublisherId, uadpDiscoveryResponse.WriterGroupId.GetValueOrDefault(), uadpDiscoveryResponse.DataSetWriterId, uadpDiscoveryResponse.DataSetClassId, uadpDiscoveryResponse.DataSetMetaData);
                }
            }
            else if ((encodedMessage is JsonNetworkMessage) || (encodedMessage is UadpNetworkMessage))
            {
                Dictionary<string, DataValue> flattenedPublishedNodes = new();
                for (int m = 0; m < encodedMessage.DataSetMessages.Count; m++)
                {
                    PubSubDataSetMessage datasetmessage = encodedMessage.DataSetMessages[m];
                    if (datasetmessage == null)
                    {
                        continue;
                    }

                    // JSON dataset messages without a network message header carry their own publisher ID
                    PublisherId publisherId = encodedMessage.PublisherId;
                    if ((datasetmessage is JsonDataSetMessage jsonDataSetMessage) && !jsonDataSetMessage.PublisherId.IsNull)
                    {
                        publisherId = jsonDataSetMessage.PublisherId;
                    }

                    string publisherID = publisherId.ToString();
                    string dataSetWriterId = datasetmessage.DataSetWriterId.ToString();
                    string assetName = string.Empty;

                    _metaData.TryGetValue(publisherID + ":" + dataSetWriterId, out DataSetMetaDataType metaData);
                    if (metaData != null)
                    {
                        string name = metaData.Name ?? string.Empty;
                        if (name.IndexOf(";") != -1)
                        {
                            assetName = name.Substring(0, name.LastIndexOf(';'));
                        }
                        else
                        {
                            assetName = name;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IGNORE_MISSING_METADATA")))
                        {
                            // if we didn't reveice a valid asset name, we use the publisher ID instead, if configured by the user
                            assetName = publisherID;
                        }
                        else
                        {
                            Console.WriteLine($"No metadata message for {publisherID}:{dataSetWriterId} received yet!");
                            continue;
                        }
                    }

                    for (int i = 0; i < datasetmessage.Fields.Count; i++)
                    {
                        DataSetField field = datasetmessage.Fields[i];
                        if (field != null)
                        {
                            // if the timestamp in the field is missing, use the timestamp from the dataset message instead
                            DateTimeUtc sourceTimestamp = field.SourceTimestamp;
                            if (sourceTimestamp.IsNull)
                            {
                                sourceTimestamp = datasetmessage.Timestamp;
                            }

                            // if we didn't receive valid metadata, we use the dataset writer ID and index into the dataset instead
                            string telemetryName = string.Empty;

                            FieldMetaData fieldMetaData = FindFieldMetaData(metaData, field.Name);
                            if (fieldMetaData == null || string.IsNullOrEmpty(fieldMetaData.Name))
                            {
                                telemetryName = assetName + "_" + datasetmessage.DataSetWriterId.ToString() + "_" + i.ToString();
                            }
                            else
                            {
                                telemetryName = assetName + "_" + fieldMetaData.Name + "_" + fieldMetaData.BinaryEncodingId.ToString();
                            }

                            try
                            {
                                // check for variant array
                                Variant fieldValue = field.Value;
                                if (fieldValue.TypeInfo.IsArray && (fieldValue.TypeInfo.BuiltInType == BuiltInType.Variant))
                                {
                                    // convert to string
                                    fieldValue = new Variant(fieldValue.ToString());
                                }

                                if (!flattenedPublishedNodes.ContainsKey(telemetryName))
                                {
                                    flattenedPublishedNodes.Add(telemetryName, new DataValue(fieldValue, field.StatusCode, sourceTimestamp));
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Cannot parse field {field.Value}: {ex.Message}");
                            }
                        }
                    }
                }

                SendPublishedNodestoSignalRHub(flattenedPublishedNodes, receivedTime);
            }
        }

        private static FieldMetaData FindFieldMetaData(DataSetMetaDataType metaData, string fieldName)
        {
            if ((metaData == null) || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            for (int i = 0; i < metaData.Fields.Count; i++)
            {
                if ((metaData.Fields[i] != null) && (metaData.Fields[i].Name == fieldName))
                {
                    return metaData.Fields[i];
                }
            }

            return null;
        }

        private void SendPublishedNodestoSignalRHub(Dictionary<string, DataValue> publishedNodes, DateTime enqueueTime)
        {
            Dictionary<string, string> displayNameMap = new Dictionary<string, string>(); // TODO: Add display name substitudes here!

            foreach (string nodeId in publishedNodes.Keys)
            {
                // substitude the node Id with a custom display name, if available
                string displayName = nodeId;
                try
                {
                    if (displayNameMap.Count > 0)
                    {
                        displayName = displayNameMap[nodeId];
                    }
                }
                catch
                {
                    // keep the original node ID as the display name
                }

                DataValue dataValue = publishedNodes[nodeId];
                if (!dataValue.IsNull)
                {
                    if (dataValue.SourceTimestamp.IsNull)
                    {
                        // use the enqueued time if the OPC UA timestamp is not present
                        dataValue = dataValue.WithSourceTimestamp(enqueueTime);
                    }

                    try
                    {
                        string timeStamp = dataValue.SourceTimestamp.ToDateTime().ToString();
                        if (!dataValue.WrappedValue.IsNull)
                        {
                            string value = dataValue.WrappedValue.ToString();

                            lock (_hubClient.TableEntries)
                            {
                                if (_hubClient.TableEntries.ContainsKey(displayName))
                                {
                                    _hubClient.TableEntries[displayName] = new Tuple<string, string>(value, timeStamp);
                                }
                                else
                                {
                                    _hubClient.TableEntries.Add(displayName, new Tuple<string, string>(value, timeStamp));
                                }

                                float floatValue;
                                if (float.TryParse(value, out floatValue))
                                {
                                    // create a keys array as index from our display names
                                    List<string> keys = new List<string>();
                                    foreach (string displayNameAsKey in _hubClient.TableEntries.Keys)
                                    {
                                        keys.Add(displayNameAsKey);
                                    }

                                    // check if we have to create an initially blank entry first
                                    if (!_hubClient.ChartEntries.ContainsKey(timeStamp) || (keys.Count != _hubClient.ChartEntries[timeStamp].Length))
                                    {
                                        string[] blankValues = new string[_hubClient.TableEntries.Count];
                                        for (int i = 0; i < blankValues.Length; i++)
                                        {
                                            blankValues[i] = "NaN";
                                        }

                                        if (_hubClient.ChartEntries.ContainsKey(timeStamp))
                                        {
                                            _hubClient.ChartEntries.Remove(timeStamp);
                                        }

                                        _hubClient.ChartEntries.Add(timeStamp, blankValues);
                                    }

                                    _hubClient.ChartEntries[timeStamp][keys.IndexOf(displayName)] = floatValue.ToString();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ignore this item
                        Trace.TraceInformation($"Cannot add item {nodeId}: {ex.Message}");
                    }
                }
            }
        }
    }
}
