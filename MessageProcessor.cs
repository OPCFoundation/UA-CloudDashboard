using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Newtonsoft.Json;
using Opc.Ua;
using OpcUaWebDashboard.Controllers;
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
        /// <summary>
        /// Opens the event processing for the given partition context.
        /// </summary>
        public Task OpenAsync(PartitionContext context)
        {
            // get number of messages between checkpoints
            _checkpointStopwatch = new Stopwatch();
            _checkpointStopwatch.Start();

            if (_sessionUpdateStopwatch == null)
            {
                _sessionUpdateStopwatch = new Stopwatch();
            }
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

            if (_sessionUpdateStopwatch != null)
            {
                _sessionUpdateStopwatch = null;
            }
        }

        private void ProcessPublisherMessage(OpcUaPubSubJsonMessage publisherMessage)
        {
            foreach (Message message in publisherMessage.Messages)
            {
                foreach (string nodeId in message.Payload.Keys)
                {
                    // get the OPC UA node object
                    ContosoOpcUaNode opcUaNode = ContosoTopology.Topology.GetOpcUaNode(message.DataSetWriterId, nodeId);
                    if (opcUaNode == null)
                    {
                        continue;
                    }

                    // Update last value.
                    opcUaNode.LastValue = message.Payload[nodeId].Value.ToString();
                    opcUaNode.LastValueTimestamp = DateTime.UtcNow.ToString();
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
            _sessionUpdateStopwatch.Start();

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
                        _processorHostMessages++;
                        // support batched messages as well as simple message ingests
                        if (message.StartsWith("["))
                        {
                            List<OpcUaPubSubJsonMessage> publisherMessages = JsonConvert.DeserializeObject<List<OpcUaPubSubJsonMessage>>(message);
                            foreach (OpcUaPubSubJsonMessage publisherMessage in publisherMessages)
                            {
                                if (publisherMessage != null)
                                {
                                    _publisherMessages++;
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
                            _publisherMessagesInvalidFormat++;
                            if (publisherMessage != null)
                            {
                                ProcessPublisherMessage(publisherMessage);
                            }
                        }

                        // if there are sessions looking at stations we update sessions each second
                        if (_sessionUpdateStopwatch.ElapsedMilliseconds > TimeSpan.FromSeconds(1).TotalMilliseconds)
                        {
                            if (DashboardController.SessionsViewingStationsCount() != 0)
                            {
                                try
                                {
                                    Trace.TraceInformation($"processorHostMessages {_processorHostMessages}, publisherMessages {_publisherMessages}/{_publisherMessagesInvalidFormat}");
                                }
                                catch (Exception e)
                                {
                                    Trace.TraceError($"Exception '{e.Message}' while updating browser sessions");
                                }
                                finally
                                {
                                    _sessionUpdateStopwatch.Restart();
                                }
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

        private Stopwatch _checkpointStopwatch;
        private const double _checkpointPeriodInMinutes = 5;
        private Stopwatch _sessionUpdateStopwatch;
        private int _processorHostMessages;
        private int _publisherMessages;
        private int _publisherMessagesInvalidFormat;
    }
}


