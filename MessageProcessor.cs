using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;
using Opc.Ua;
using OpcUaWebDashboard.Controllers;
using OpcUaWebDashboard.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaWebDashboard
{
    public class OpcUaPubSubJsonMessage
    {
        public string MessageId { get; set; }

        public string MessageType { get; set; }

        public string PublisherId { get; set; }

        public string DataSetClassId { get; set; }

        public List<Message> Messages { get; set; }
    }

    public class Message
    {
        public string DataSetWriterId { get; set; }

        public MetaDataVersion MetaDataVersion { get; set; }

        public Dictionary<string, DataValue> Payload { get; set; }
    }

    public class MetaDataVersion
    {
        public int MajorVersion { get; set; }

        public int MinorVersion { get; set; }
    }

    /// <summary>
    /// This class processes all ingested data into IoTHub.
    /// </summary>
    public class MessageProcessor : IEventProcessor
    {
        private Stopwatch _checkpointStopwatch;
        private const double _checkpointPeriodInMinutes = 5;
        private List<string> _nodeIDs = new List<string>();
        
        public Task OpenAsync(PartitionContext context)
        {
            // get number of messages between checkpoints
            _checkpointStopwatch = new Stopwatch();
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

        private void ProcessPublisherMessage(OpcUaPubSubJsonMessage publisherMessage)
        {
            List<SignalRModel> receivedDataItems = new List<SignalRModel>();
            _nodeIDs.Clear();

            // unbatch the received data
            foreach (Message message in publisherMessage.Messages)
            {
                foreach (string nodeId in message.Payload.Keys)
                {
                    // make sure we have it in our list of nodeIDs, which form the basis of our individual time series datasets
                    if (!_nodeIDs.Contains(nodeId))
                    {
                        _nodeIDs.Add(nodeId);
                        DashboardController.AddDatasetToChart(nodeId);
                    }

                    // try to add to our list of received values
                    try
                    {
                        SignalRModel newItem = new SignalRModel {
                            NodeID = nodeId,
                            TimeStamp = message.Payload[nodeId].SourceTimestamp,
                            Value = float.Parse(message.Payload[nodeId].Value.ToString())
                        };
                        receivedDataItems.Add(newItem);
                    }
                    catch (Exception)
                    {
                        // ignore this item
                    }
                }
            }

            // group messages by timestamp in nodeID,value pairs
            List<Tuple<string,float>> currentValues = new List<Tuple<string,float>>();

            // add first entry
            if (receivedDataItems.Count > 0)
            {
                DateTime currentTimestamp = receivedDataItems[0].TimeStamp;
                currentValues.Add(new Tuple<string, float>(receivedDataItems[0].NodeID, receivedDataItems[0].Value));
                receivedDataItems.RemoveAt(0);


                // add the rest, per timestamp
                while (receivedDataItems.Count > 0)
                {
                    for (int i = 0; i < receivedDataItems.Count; i++)
                    {
                        if (receivedDataItems[i].TimeStamp == currentTimestamp)
                        {
                            currentValues.Add(new Tuple<string, float>(receivedDataItems[i].NodeID, receivedDataItems[i].Value));
                            receivedDataItems.RemoveAt(i);
                            i--;
                        }
                    }

                    // fill the blank datasets for this timestamp with nulls
                    float[] values = new float[_nodeIDs.Count];
                    for (int i = 0; i < values.Length; i++)
                    {
                        // init to NaN
                        values[i] = float.NaN;

                        // check if we have a real value
                        foreach (Tuple<string, float> item in currentValues)
                        {
                            if (item.Item1 == _nodeIDs[i])
                            {
                                values[i] = item.Item2;
                                break;
                            }
                        }
                    }

                    // add entry for current timestamp to chart
                    DashboardController.AddDataToChart(currentTimestamp.ToString(), values);

                    // add next entry
                    if (receivedDataItems.Count > 0)
                    {
                        currentTimestamp = receivedDataItems[0].TimeStamp;
                        currentValues.Add(new Tuple<string, float>(receivedDataItems[0].NodeID, receivedDataItems[0].Value));
                        receivedDataItems.RemoveAt(0);
                    }
                }
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
                                        ProcessPublisherMessage(publisherMessage);
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
                            OpcUaPubSubJsonMessage publisherMessage = JsonConvert.DeserializeObject<OpcUaPubSubJsonMessage>(message);
                            if (publisherMessage != null)
                            {
                                ProcessPublisherMessage(publisherMessage);
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


