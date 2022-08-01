using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;
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

        private void ProcessPublisherMessage(OpcUaPubSubJsonMessage publisherMessage)
        {
            List<SignalRModel> receivedDataItems = new List<SignalRModel>();
            List<Tuple<string, string, string>> tableEntries = new List<Tuple<string, string, string>>();

            string samsonURI = "http://samsongroup.com/opc_ua_SAM_FLEXPOS_DIM/";
            Dictionary<string, string> displayNameMap = new Dictionary<string, string>();
            displayNameMap.Add(samsonURI + "#i=6076", "SAMSON Trovis ActDynStressFactor (i=6076)");
            displayNameMap.Add(samsonURI + "#i=6112", "SAMSON Trovis DiagnosticStatus (i=6112)");
            displayNameMap.Add(samsonURI + "#i=6092", "SAMSON Trovis ActValvePosition (i=6092)");
            displayNameMap.Add(samsonURI + "#i=6066", "SAMSON Trovis ActPressureOut2 (i=6066)");
            displayNameMap.Add(samsonURI + "#i=6061", "SAMSON Trovis ActPressureOut1 (i=6061)");
            displayNameMap.Add(samsonURI + "#i=6071", "SAMSON Trovis ActSupplyPressure (i=6071)");
            displayNameMap.Add(samsonURI + "#i=6102", "SAMSON Trovis SetValvePosition (i=6102)");
            displayNameMap.Add(samsonURI + "#i=6097", "SAMSON Trovis ActControlDeviation (i=6097)");

            // unbatch the received data
            if (publisherMessage.Messages != null)
            {
                foreach (Message message in publisherMessage.Messages)
                {
                    foreach (string nodeId in message.Payload.Keys)
                    {
                        string displayName = nodeId;
                        DateTime timestamp = DateTime.UtcNow;

                        // check our map first
                        if (displayNameMap.ContainsKey(nodeId))
                        {
                            displayName = displayNameMap[nodeId];
                        }

                        // check timestamp
                        if (message.Payload[nodeId].SourceTimestamp != DateTime.MinValue)
                        {
                            timestamp = message.Payload[nodeId].SourceTimestamp;
                        }

                        // make sure we have it in our list of nodeIDs, which form the basis of our individual time series datasets
                        if (!NodeIDs.Contains(displayName))
                        {
                            NodeIDs.Add(displayName);
                            DashboardController.AddDatasetToChart(displayName);
                        }

                        // try to add to our list of received values
                        try
                        {
                            SignalRModel newItem = new SignalRModel
                            {
                                NodeID = displayName,
                                TimeStamp = timestamp,
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
                            timestamp.ToString()
                        ));
                    }
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


