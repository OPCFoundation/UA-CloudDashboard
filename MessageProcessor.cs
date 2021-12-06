using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.PubSub;
using Opc.Ua.PubSub.Encoding;
using Opc.Ua.PubSub.PublishedData;
using OpcUaWebDashboard.Controllers;
using OpcUaWebDashboard.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaWebDashboard
{
    /// <summary>
    /// This class processes all ingested data into IoTHub.
    /// </summary>
    public class MessageProcessor : IEventProcessor
    {
        private Stopwatch _checkpointStopwatch = new Stopwatch();
        private const double _checkpointPeriodInMinutes = 5;

        public static List<string> NodeIDs = new List<string>();

        public Task OpenAsync(PartitionContext context)
        {
            // get number of messages between checkpoints
            _checkpointStopwatch.Start();

            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Trace.TraceError($"Message processor error '{error.Message}' on partition with id '{context.PartitionId}'");
            Trace.TraceError($"Exception stack '{error.StackTrace}'");
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
            List<SignalRModel> receivedDataItems = new List<SignalRModel>();
            List<Tuple<string, string, string>> tableEntries = new List<Tuple<string, string, string>>();

            Dictionary<string, string> displayNameMap = new Dictionary<string, string>();
            // TODO: Add display name substitudes here!

            // unbatch the received data
            foreach (Message message in publisherMessage.Messages)
            {
                foreach (string nodeId in message.Payload.Keys)
                {
                    // substitude the node Id with a custom display name, if available
                    string displayName = nodeId;
                    try
                    {
                        displayName = displayNameMap[nodeId];
                    }
                    catch
                    {
                        // keep the original node ID as the display name
                    }

                    // make sure we have it in our list of nodeIDs, which form the basis of our individual time series datasets
                    if (!NodeIDs.Contains(displayName))
                    {
                        NodeIDs.Add(displayName);
                        DashboardController.AddDatasetToChart(displayName);
                    }

                    if (message.Payload[nodeId].SourceTimestamp == DateTime.MinValue)
                    {
                        // use the IoT Hub enqueued time if the OPC UA timestamp is not present
                        message.Payload[nodeId].SourceTimestamp = enqueueTime;
                    }

                    // try to add to our list of received values
                    try
                    {
                        SignalRModel newItem = new SignalRModel {
                            NodeID = displayName,
                            TimeStamp = message.Payload[nodeId].SourceTimestamp,
                            Value = float.Parse(message.Payload[nodeId].Value.ToString())
                        };

                        receivedDataItems.Add(newItem);
                    }
                    catch (Exception)
                    {
                        // ignore this item
                    }

                    // add item to our table entries
                    tableEntries.Add(new Tuple<string, string, string>(
                        displayName,
                        message.Payload[nodeId].Value.ToString(),
                        message.Payload[nodeId].SourceTimestamp.ToString()
                    ));
                }
            }

            // update our table in the dashboard
            DashboardController.CreateTableForTelemetry(tableEntries);

            // update our line chart in the dashboard
            string[] currentValues = new string[NodeIDs.Count];
            Array.Fill(currentValues, "NaN");
            while (receivedDataItems.Count > 0)
            {
                DateTime currentTimestamp = receivedDataItems[0].TimeStamp;

                // add first value from the start of our received data items array
                currentValues[NodeIDs.IndexOf(receivedDataItems[0].NodeID)] = receivedDataItems[0].Value.ToString();
                receivedDataItems.RemoveAt(0);

                // add the values we received with the same timestamp
                for (int i = 0; i < receivedDataItems.Count; i++)
                {
                    if (receivedDataItems[i].TimeStamp == currentTimestamp)
                    {
                        currentValues[NodeIDs.IndexOf(receivedDataItems[i].NodeID)] = receivedDataItems[i].Value.ToString();
                        receivedDataItems.RemoveAt(i);
                        i--;
                    }
                }

                DashboardController.AddDataToChart(currentTimestamp.ToString(), currentValues);
            }
        }

        private void Checkpoint(PartitionContext context, Stopwatch checkpointStopwatch)
        {
            context.CheckpointAsync();
            checkpointStopwatch.Restart();
            Trace.TraceInformation($"checkpoint completed at {DateTime.UtcNow}");
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
                    if (_checkpointStopwatch.Elapsed > TimeSpan.FromMinutes(_checkpointPeriodInMinutes))
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
                                OpcUaPubSubJsonMessage publisherMessage = JsonConvert.DeserializeObject<OpcUaPubSubJsonMessage>(message);
                                if (publisherMessage != null)
                                {
                                    ProcessPublisherMessage(publisherMessage, (DateTime)eventData.SystemProperties["iothub-enqueuedtime"]);
                                }
                            }
                            catch (Exception)
                            {
                                // try processing as UADP binary message
                                UadpNetworkMessage uaNetworkMessageDecoded = new UadpNetworkMessage();
                                List<DataSetReaderDataType> dataSetReaders = new List<DataSetReaderDataType>();
                                dataSetReaders.Add(new DataSetReaderDataType());
                                uaNetworkMessageDecoded.Decode(ServiceMessageContext.GlobalContext, eventData.Body.Array, dataSetReaders);

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

                                ProcessPublisherMessage(publisherMessage, (DateTime)eventData.SystemProperties["iothub-enqueuedtime"]);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Exception '{e.Message}' processing message '{message}'");
                }
            }
        }
    }
}


