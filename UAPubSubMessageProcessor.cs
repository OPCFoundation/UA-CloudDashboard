
namespace OpcUaWebDashboard
{
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.PubSub;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.PublishedData;
    using OpcUaWebDashboard.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    public class UAPubSubMessageProcessor : IUAPubSubMessageProcessor
    {
        private StatusHubClient _hubClient;

        public UAPubSubMessageProcessor()
        {
            IServiceProvider serviceProvider = Program.AppHost.Services;
            _hubClient = new StatusHubClient((IHubContext<StatusHub>)serviceProvider.GetService(typeof(IHubContext<StatusHub>)));
        }

        public void ProcessMessage(byte[] payload, DateTime receivedTime)
        {
            string message = string.Empty;
            try
            {
                message = Encoding.UTF8.GetString(payload);
                if (message != null)
                {
                    // support batched messages as well as simple message ingests
                    if (message.StartsWith("["))
                    {
                        List<OpcUaPubSubJsonMessage> publisherMessages = JsonConvert.DeserializeObject<List<OpcUaPubSubJsonMessage>>(message);
                        foreach (OpcUaPubSubJsonMessage publisherMessage in publisherMessages)
                        {
                            if (publisherMessage != null)
                            {
                                try
                                {
                                    ProcessPublisherMessage(publisherMessage, receivedTime);
                                }
                                catch (Exception e)
                                {
                                    Trace.TraceError($"Exception '{e.Message}' while processing message {publisherMessage}");
                                }
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            if (message.StartsWith("{"))
                            {
                                OpcUaPubSubJsonMessage publisherMessage = JsonConvert.DeserializeObject<OpcUaPubSubJsonMessage>(message);
                                if (publisherMessage != null)
                                {
                                    ProcessPublisherMessage(publisherMessage, receivedTime);
                                }
                            }
                            else
                            {
                                DeserializeUADPMessage(payload, receivedTime);
                            }
                        }
                        catch (Exception)
                        {
                            DeserializeUADPMessage(payload, receivedTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception {ex.Message} processing message {message}");
            }
        }

        private void DeserializeUADPMessage(byte[] payload, DateTime receivedTime)
        {
            // try processing as UADP binary message
            UadpNetworkMessage uaNetworkMessageDecoded = new UadpNetworkMessage();
            List<DataSetReaderDataType> dataSetReaders = new List<DataSetReaderDataType>();
            dataSetReaders.Add(new DataSetReaderDataType());
            uaNetworkMessageDecoded.Decode(ServiceMessageContext.GlobalContext, payload, dataSetReaders);

            OpcUaPubSubJsonMessage publisherMessage = new OpcUaPubSubJsonMessage();
            publisherMessage.Messages = new List<Message>();
            foreach (UaDataSetMessage datasetmessage in uaNetworkMessageDecoded.DataSetMessages)
            {
                Message pubSubMessage = new Message();
                pubSubMessage.Payload = new Dictionary<string, DataValue>();
                foreach (Field field in datasetmessage.DataSet.Fields)
                {
                    pubSubMessage.Payload.Add(uaNetworkMessageDecoded.PublisherId.ToString(), field.Value);
                }
                publisherMessage.Messages.Add(pubSubMessage);
            }

            ProcessPublisherMessage(publisherMessage, receivedTime);
        }

        private void ProcessPublisherMessage(OpcUaPubSubJsonMessage publisherMessage, DateTime enqueueTime)
        {
            Dictionary<string, string> displayNameMap = new Dictionary<string, string>(); // TODO: Add display name substitudes here!

            // unbatch the received data
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
