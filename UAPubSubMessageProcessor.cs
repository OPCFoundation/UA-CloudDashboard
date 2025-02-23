﻿
namespace Opc.Ua.Cloud.Dashboard
{
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.PublishedData;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using UACloudDashboard.Interfaces;

    public class UAPubSubMessageProcessor : IMessageProcessor
    {
        private StatusHubClient _hubClient;
        private Dictionary<string, DataSetReaderDataType> _dataSetReaders;

        public UAPubSubMessageProcessor(IHubContext<StatusHub> hubContext)
        {
            _hubClient = new StatusHubClient(hubContext);
            _dataSetReaders = new Dictionary<string, DataSetReaderDataType>();

            // add default dataset readers
            AddUadpDataSetReader("default_uadp", 0, new DataSetMetaDataType());
            AddJsonDataSetReader("default_json", 0, new DataSetMetaDataType());
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
                            object[] messageArray = JsonConvert.DeserializeObject<object[]>(message);
                            foreach (object singleMessage in messageArray)
                            {
                                DecodeMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(singleMessage)), receivedTime, new JsonNetworkMessage());
                            }
                        }
                        else
                        {
                            DecodeMessage(payload, receivedTime, new JsonNetworkMessage());
                        }
                    }
                    else
                    {
                        DecodeMessage(payload, receivedTime, new UadpNetworkMessage());
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception {ex.Message} processing message {message}");
            }
        }

        private void AddUadpDataSetReader(string publisherId, ushort dataSetWriterId, DataSetMetaDataType metadata)
        {
            DataSetReaderDataType uadpDataSetReader = new DataSetReaderDataType();
            uadpDataSetReader.Name = publisherId + ":" + dataSetWriterId.ToString();
            uadpDataSetReader.DataSetWriterId = dataSetWriterId;
            uadpDataSetReader.PublisherId = publisherId;
            uadpDataSetReader.Enabled = true;
            uadpDataSetReader.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None;
            uadpDataSetReader.KeyFrameCount = 1;
            uadpDataSetReader.TransportSettings = new ExtensionObject(new BrokerDataSetReaderTransportDataType());
            uadpDataSetReader.DataSetMetaData = metadata;

            UadpDataSetReaderMessageDataType uadpDataSetReaderMessageSettings = new UadpDataSetReaderMessageDataType()
            {
                NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.NetworkMessageNumber | UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.DataSetClassId),
                DataSetMessageContentMask = (uint)UadpDataSetMessageContentMask.None,
            };

            uadpDataSetReader.MessageSettings = new ExtensionObject(uadpDataSetReaderMessageSettings);

            TargetVariablesDataType subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            uadpDataSetReader.SubscribedDataSet = new ExtensionObject(subscribedDataSet);

            if (_dataSetReaders.ContainsKey(uadpDataSetReader.Name))
            {
                _dataSetReaders[uadpDataSetReader.Name] = uadpDataSetReader;
            }
            else
            {
                _dataSetReaders.Add(uadpDataSetReader.Name, uadpDataSetReader);
            }
        }

        private void AddJsonDataSetReader(string publisherId, ushort dataSetWriterId, DataSetMetaDataType metadata)
        {
            DataSetReaderDataType jsonDataSetReader = new DataSetReaderDataType();
            jsonDataSetReader.Name = publisherId + ":" + dataSetWriterId.ToString();
            jsonDataSetReader.PublisherId = publisherId;
            jsonDataSetReader.DataSetWriterId = dataSetWriterId;
            jsonDataSetReader.Enabled = true;
            jsonDataSetReader.DataSetFieldContentMask = (uint)DataSetFieldContentMask.None;
            jsonDataSetReader.KeyFrameCount = 1;
            jsonDataSetReader.TransportSettings = new ExtensionObject(new BrokerDataSetReaderTransportDataType());
            jsonDataSetReader.DataSetMetaData = metadata;

            JsonDataSetReaderMessageDataType jsonDataSetReaderMessageSettings = new JsonDataSetReaderMessageDataType()
            {
                NetworkMessageContentMask = (uint)(JsonNetworkMessageContentMask.NetworkMessageHeader | JsonNetworkMessageContentMask.DataSetMessageHeader | JsonNetworkMessageContentMask.DataSetClassId | JsonNetworkMessageContentMask.PublisherId),
                DataSetMessageContentMask = (uint)JsonDataSetMessageContentMask.None,
            };

            jsonDataSetReader.MessageSettings = new ExtensionObject(jsonDataSetReaderMessageSettings);

            TargetVariablesDataType subscribedDataSet = new TargetVariablesDataType();
            subscribedDataSet.TargetVariables = new FieldTargetDataTypeCollection();
            jsonDataSetReader.SubscribedDataSet = new ExtensionObject(subscribedDataSet);

            if (_dataSetReaders.ContainsKey(jsonDataSetReader.Name))
            {
                _dataSetReaders[jsonDataSetReader.Name] = jsonDataSetReader;
            }
            else
            {
                _dataSetReaders.Add(jsonDataSetReader.Name, jsonDataSetReader);
            }
        }

        private void DecodeMessage(byte[] payload, DateTime receivedTime, UaNetworkMessage encodedMessage)
        {
            encodedMessage.Decode(ServiceMessageContext.GlobalContext, payload, null);
            if (encodedMessage.IsMetaDataMessage)
            {
                // setup dataset reader
                if (encodedMessage is JsonNetworkMessage)
                {
                    JsonNetworkMessage jsonMessage = (JsonNetworkMessage)encodedMessage;

                    AddJsonDataSetReader(jsonMessage.PublisherId, jsonMessage.DataSetWriterId, encodedMessage.DataSetMetaData);
                }
                else
                {
                    UadpNetworkMessage uadpMessage = (UadpNetworkMessage)encodedMessage;
                    AddUadpDataSetReader(uadpMessage.PublisherId.ToString(), uadpMessage.DataSetWriterId, encodedMessage.DataSetMetaData);
                }
            }
            else
            {
                encodedMessage.Decode(ServiceMessageContext.GlobalContext, payload, _dataSetReaders.Values.ToArray());

                // reset metadata fields on default dataset readers
                _dataSetReaders["default_uadp:0"].DataSetMetaData.Fields.Clear();
                _dataSetReaders["default_json:0"].DataSetMetaData.Fields.Clear();

                string publisherID = string.Empty;
                if (encodedMessage is JsonNetworkMessage)
                {
                    publisherID = ((JsonNetworkMessage)encodedMessage).PublisherId?.ToString();
                }
                else
                {
                    publisherID = ((UadpNetworkMessage)encodedMessage).PublisherId?.ToString();
                }

                Dictionary<string, DataValue> flattenedPublishedNodes = new();
                foreach (UaDataSetMessage datasetmessage in encodedMessage.DataSetMessages)
                {
                    string dataSetWriterId = datasetmessage.DataSetWriterId.ToString();
                    string assetName = string.Empty;

                    if (_dataSetReaders.ContainsKey(publisherID + ":" + dataSetWriterId))
                    {
                        string name = _dataSetReaders[publisherID + ":" + dataSetWriterId].DataSetMetaData.Name;
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

                    if (datasetmessage.DataSet != null)
                    {
                        for (int i = 0; i < datasetmessage.DataSet.Fields.Length; i++)
                        {
                            Field field = datasetmessage.DataSet.Fields[i];
                            if (field.Value != null)
                            {
                                // if the timestamp in the field is missing, use the timestamp from the dataset message instead
                                if (field.Value.SourceTimestamp == DateTime.MinValue)
                                {
                                    field.Value.SourceTimestamp = datasetmessage.Timestamp;
                                }

                                // if we didn't receive valid metadata, we use the dataset writer ID and index into the dataset instead
                                string telemetryName = string.Empty;

                                if (field.FieldMetaData == null || string.IsNullOrEmpty(field.FieldMetaData.Name))
                                {
                                    telemetryName = assetName + "_" + datasetmessage.DataSetWriterId.ToString() + "_" + i.ToString();
                                }
                                else
                                {
                                    telemetryName = assetName + "_" + field.FieldMetaData.Name + "_" + field.FieldMetaData.BinaryEncodingId.ToString();
                                }

                                try
                                {
                                    // check for variant array
                                    if (field.Value.Value is Variant[])
                                    {
                                        // convert to string
                                        DataValue value = new DataValue(new Variant(field.Value.ToString()), field.Value.StatusCode, field.Value.SourceTimestamp);

                                        if (!flattenedPublishedNodes.ContainsKey(telemetryName))
                                        {
                                            flattenedPublishedNodes.Add(telemetryName, value);
                                        }
                                    }
                                    else
                                    {
                                        if (!flattenedPublishedNodes.ContainsKey(telemetryName))
                                        {
                                            flattenedPublishedNodes.Add(telemetryName, field.Value);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Cannot parse field {field.Value}: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                SendPublishedNodestoSignalRHub(flattenedPublishedNodes, receivedTime);
            }
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

                if (publishedNodes[nodeId] != null)
                {
                    if (publishedNodes[nodeId].SourceTimestamp == DateTime.MinValue)
                    {
                        // use the enqueued time if the OPC UA timestamp is not present
                        publishedNodes[nodeId].SourceTimestamp = enqueueTime;
                    }

                    try
                    {
                        string timeStamp = publishedNodes[nodeId].SourceTimestamp.ToString();
                        if (publishedNodes[nodeId].Value != null)
                        {
                            string value = publishedNodes[nodeId].Value.ToString();

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
