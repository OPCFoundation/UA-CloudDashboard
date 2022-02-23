
namespace OpcUaWebDashboard
{
    using Microsoft.AspNetCore.SignalR;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.PublishedData;
    using OpcUaWebDashboard.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public class UAPubSubMessageProcessor : IUAPubSubMessageProcessor
    {
        private StatusHubClient _hubClient;
        private Dictionary<string, DataSetReaderDataType> _dataSetReaders;

        public UAPubSubMessageProcessor()
        {
            IServiceProvider serviceProvider = Program.AppHost.Services;
            _hubClient = new StatusHubClient((IHubContext<StatusHub>)serviceProvider.GetService(typeof(IHubContext<StatusHub>)));
            _dataSetReaders = new Dictionary<string, DataSetReaderDataType>();
        }

        public void ProcessMessage(byte[] payload, DateTime receivedTime, string contentType)
        {
            string message = string.Empty;
            try
            {
                message = Encoding.UTF8.GetString(payload);
                if (message != null)
                {
                    if ((contentType != null) && (contentType == "application/json") || message.StartsWith('{'))
                    { 
                        DecodeMessage(payload, receivedTime, new JsonNetworkMessage());
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

        private void DecodeMessage(byte[] payload, DateTime receivedTime, UaNetworkMessage encodedMessage)
        {
            encodedMessage.Decode(ServiceMessageContext.GlobalContext, payload, null);
            if (encodedMessage.IsMetaDataMessage)
            {
                // setup dataset reader
                if (encodedMessage is JsonNetworkMessage)
                {
                    DataSetReaderDataType jsonDataSetReader = new DataSetReaderDataType();
                    jsonDataSetReader.Name = "dataSetReader";
                    jsonDataSetReader.Enabled = true;
                    jsonDataSetReader.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
                    jsonDataSetReader.KeyFrameCount = 1;
                    jsonDataSetReader.TransportSettings = new ExtensionObject(new BrokerDataSetReaderTransportDataType());
                    jsonDataSetReader.DataSetMetaData = encodedMessage.DataSetMetaData;

                    DataSetReaderMessageDataType jsonDataSetReaderMessageSettings = new UadpDataSetReaderMessageDataType()
                    {
                        NetworkMessageContentMask = (uint)(UadpNetworkMessageContentMask.PublisherId | UadpNetworkMessageContentMask.WriterGroupId),
                        DataSetMessageContentMask = (uint)UadpDataSetMessageContentMask.None,
                    };
                    jsonDataSetReader.MessageSettings = new ExtensionObject(jsonDataSetReaderMessageSettings);

                    TargetVariablesDataType subscribedDataSet1 = new TargetVariablesDataType();
                    subscribedDataSet1.TargetVariables = new FieldTargetDataTypeCollection();
                    jsonDataSetReader.SubscribedDataSet = new ExtensionObject(subscribedDataSet1);

                    if (_dataSetReaders.ContainsKey(((JsonNetworkMessage)encodedMessage).PublisherId))
                    {
                        _dataSetReaders[((JsonNetworkMessage)encodedMessage).PublisherId] = jsonDataSetReader;
                    }
                    else
                    {
                        _dataSetReaders.Add(((JsonNetworkMessage)encodedMessage).PublisherId, jsonDataSetReader);
                    }
                }
                else
                {
                    DataSetReaderDataType uadpDataSetReader = new DataSetReaderDataType();
                    uadpDataSetReader.Name = "dataSetReader";
                    uadpDataSetReader.Enabled = true;
                    uadpDataSetReader.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
                    uadpDataSetReader.KeyFrameCount = 1;
                    uadpDataSetReader.TransportSettings = new ExtensionObject(new BrokerDataSetReaderTransportDataType());
                    uadpDataSetReader.DataSetMetaData = encodedMessage.DataSetMetaData;

                    DataSetReaderMessageDataType uadpDataSetReaderMessageSettings = new JsonDataSetReaderMessageDataType()
                    {
                        NetworkMessageContentMask = (uint)(JsonNetworkMessageContentMask.PublisherId | JsonNetworkMessageContentMask.NetworkMessageHeader),
                        DataSetMessageContentMask = (uint)JsonDataSetMessageContentMask.None,
                    };
                    uadpDataSetReader.MessageSettings = new ExtensionObject(uadpDataSetReaderMessageSettings);

                    TargetVariablesDataType subscribedDataSet2 = new TargetVariablesDataType();
                    subscribedDataSet2.TargetVariables = new FieldTargetDataTypeCollection();
                    uadpDataSetReader.SubscribedDataSet = new ExtensionObject(subscribedDataSet2);

                    if (!_dataSetReaders.ContainsKey(((UadpNetworkMessage)encodedMessage).PublisherId.ToString()))
                    {
                        _dataSetReaders.Add(((UadpNetworkMessage)encodedMessage).PublisherId.ToString(), uadpDataSetReader);
                    }
                }
            }
            else
            {
                encodedMessage.Decode(ServiceMessageContext.GlobalContext, payload, _dataSetReaders.Values.ToArray());

                string publisherID;
                if (encodedMessage is JsonNetworkMessage)
                {
                    publisherID = ((JsonNetworkMessage)encodedMessage).PublisherId.ToString();
                }
                else
                {
                    publisherID = ((UadpNetworkMessage)encodedMessage).PublisherId.ToString();
                }

                OpcUaPubSubMessageModel publisherMessage = new OpcUaPubSubMessageModel();
                publisherMessage.Messages = new List<Message>();
                foreach (UaDataSetMessage datasetmessage in encodedMessage.DataSetMessages)
                {
                    Message pubSubMessage = new Message();
                    pubSubMessage.Payload = new Dictionary<string, DataValue>();
                    if (datasetmessage.DataSet != null)
                    {
                        foreach (Field field in datasetmessage.DataSet.Fields)
                        {
                            pubSubMessage.Payload.Add(publisherID + "_" + field.FieldMetaData.Name, field.Value);
                        }
                        publisherMessage.Messages.Add(pubSubMessage);
                    }
                }

                ProcessPublisherMessage(publisherMessage, receivedTime);
            }
        }

        private void ProcessPublisherMessage(OpcUaPubSubMessageModel publisherMessage, DateTime enqueueTime)
        {
            Dictionary<string, string> displayNameMap = new Dictionary<string, string>(); // TODO: Add display name substitudes here!

            // unbatch the received data
            if (publisherMessage.Messages != null)
            {
                foreach (Message message in publisherMessage.Messages)
                {
                    foreach (string nodeId in message.Payload.Keys)
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

                        if (message.Payload[nodeId] != null)
                        {
                            if (message.Payload[nodeId].SourceTimestamp == DateTime.MinValue)
                            {
                                // use the IoT Hub enqueued time if the OPC UA timestamp is not present
                                message.Payload[nodeId].SourceTimestamp = enqueueTime;
                            }

                            try
                            {
                                string timeStamp = message.Payload[nodeId].SourceTimestamp.ToString();
                                string value = message.Payload[nodeId].Value.ToString();

                                lock (_hubClient.TableEntries)
                                {
                                    if (_hubClient.TableEntries.ContainsKey(displayName))
                                    {
                                        _hubClient.TableEntries[displayName] = new Tuple<string, string>(value, timeStamp);
                                    }
                                    else
                                    {
                                        _hubClient.TableEntries.TryAdd(displayName, new Tuple<string, string>(value, timeStamp));
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
    }
}
