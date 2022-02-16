using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpcUaWebDashboard
{
    public class EventHubProcessor : IEventProcessor
    {
        private readonly IUAPubSubMessageProcessor _uaMessageProcessor;

        private Stopwatch _checkpointStopwatch = new Stopwatch();

        public EventHubProcessor()
        {
            _uaMessageProcessor = (IUAPubSubMessageProcessor)Program.AppHost.Services.GetService(typeof(IUAPubSubMessageProcessor));
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
                await context.CheckpointAsync().ConfigureAwait(false);
            }
        }

        private void Checkpoint(PartitionContext context)
        {
            context.CheckpointAsync();

            _checkpointStopwatch.Restart();

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
                        await Task.Run(() => Checkpoint(context)).ConfigureAwait(false);
                    }

                    byte[] payload = new byte[eventData.Body.Count];
                    Array.Copy(eventData.Body.Array, eventData.Body.Offset, payload, 0, eventData.Body.Count);
                    _uaMessageProcessor.ProcessMessage(payload, (DateTime)eventData.SystemProperties["iothub-enqueuedtime"]);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Exception {e.Message} processing message {message}");
                }
            }
        }
    }
}


