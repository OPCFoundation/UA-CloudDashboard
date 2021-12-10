using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
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
using System.Threading.Tasks;

namespace OpcUaWebDashboard
{
    public class MessageProcessor : IEventProcessor
    {
        private Stopwatch _checkpointStopwatch = new Stopwatch();

        private StatusHub _hub;

        public MessageProcessor()
        {
            IServiceProvider serviceProvider = Program.AppHost.Services;
            _hub = (StatusHub) serviceProvider.GetService(typeof(StatusHub));
        }

        public Task OpenAsync(PartitionContext context)
        {
            _checkpointStopwatch.Start();

            Trace.TraceInformation($"EventProcessor successfully registered for partition {context.PartitionId}.");

            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Trace.TraceError($"Message processor error {error.Message} on partition with id {context.PartitionId}.");

            return Task.CompletedTask;
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
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

                        lock (_hub.TableEntries)
                        {
                            if (_hub.TableEntries.ContainsKey(displayName))
                            {
                                _hub.TableEntries[displayName] = new Tuple<string, string>(value, timeStamp);
                            }
                            else
                            {
                                _hub.TableEntries.TryAdd(displayName, new Tuple<string, string>(value, timeStamp));
                            }

                            float floatValue;
                            if (float.TryParse(value, out floatValue))
                            {
                                // create a keys array as index from our display names
                                List<string> keys = new List<string>();
                                foreach (string displayNameAsKey in _hub.TableEntries.Keys)
                                {
                                    keys.Add(displayNameAsKey);
                                }

                                // check if we have to create an initially blank entry first
                                if (!_hub.ChartEntries.ContainsKey(timeStamp) || (keys.Count != _hub.ChartEntries.Count))
                                {
                                    string[] blankValues = new string[_hub.TableEntries.Count];
                                    for (int i = 0; i < blankValues.Length; i++)
                                    {
                                        blankValues[i] = "NaN";
                                    }

                                    if (_hub.ChartEntries.ContainsKey(timeStamp))
                                    {
                                        _hub.ChartEntries.Remove(timeStamp);
                                    }

                                    _hub.ChartEntries.Add(timeStamp, blankValues);
                                }

                                _hub.ChartEntries[timeStamp][keys.IndexOf(displayName)] = floatValue.ToString();
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

        private void Checkpoint(PartitionContext context, Stopwatch checkpointStopwatch)
        {
            context.CheckpointAsync();

            checkpointStopwatch.Restart();

            Trace.TraceInformation($"checkpoint for partition {context.PartitionId} completed at {DateTime.UtcNow}.");
        }

        /// <summary>
        /// Process all events from OPC UA servers and update the last value of each node in the topology.
        /// </summary>
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> ingestedMessages)
        {
            // process each message
            foreach (var eventData in ingestedMessages)
            {
                string message = null;
                try
                {
                    // checkpoint, so that the processor does not need to start from the beginning if it restarts
                    if (_checkpointStopwatch.Elapsed > TimeSpan.FromMinutes(5))
                    {
                        await Task.Run(() => Checkpoint(context, _checkpointStopwatch)).ConfigureAwait(false);
                    }

                    message = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
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
                                        ProcessPublisherMessage(publisherMessage, (DateTime) eventData.SystemProperties["iothub-enqueuedtime"]);
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
                                        ProcessPublisherMessage(publisherMessage, (DateTime)eventData.SystemProperties["iothub-enqueuedtime"]);
                                    }
                                }
                                else
                                {
                                    DeserializeUADPMessage(eventData);
                                }
                            }
                            catch (Exception)
                            {
                                DeserializeUADPMessage(eventData);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Exception {e.Message} processing message {message}");
                }
            }
        }

        private void DeserializeUADPMessage(EventData message)
        {
            // try processing as UADP binary message
            UadpNetworkMessage uaNetworkMessageDecoded = new UadpNetworkMessage();
            List<DataSetReaderDataType> dataSetReaders = new List<DataSetReaderDataType>();
            dataSetReaders.Add(new DataSetReaderDataType());
            uaNetworkMessageDecoded.Decode(ServiceMessageContext.GlobalContext, message.Body.Array, dataSetReaders);

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

            ProcessPublisherMessage(publisherMessage, (DateTime)message.SystemProperties["iothub-enqueuedtime"]);
        }
    }
}


