
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OpcUaWebDashboard
{
    public class IoTHubConfig
    {
        public static Task ConfigureIotHub()
        {
            return Task.Run(() => ConnectToIotHubAsync(CancellationToken.None));
        }

        private static async Task ConnectToIotHubAsync(CancellationToken ct)
        {
            EventProcessorHost eventProcessorHost;

            // Get configuration settings
            string iotHubTelemetryConsumerGroup = "dashboard";
            string iotHubEventHubName = Environment.GetEnvironmentVariable("IotHubEventHubName");
            string iotHubEventHubEndpointIotHubOwnerConnectionString = Environment.GetEnvironmentVariable("EventHubEndpointIotHubOwnerConnectionString");
            string solutionStorageAccountConnectionString = Environment.GetEnvironmentVariable("StorageAccountConnectionString");

            // Initialize EventProcessorHost.
            Trace.TraceInformation("Creating EventProcessorHost for IoTHub: {0}, ConsumerGroup: {1}, ConnectionString: {2}, StorageConnectionString: {3}",
                iotHubEventHubName, iotHubTelemetryConsumerGroup, iotHubEventHubEndpointIotHubOwnerConnectionString, solutionStorageAccountConnectionString);

            string StorageContainerName = "telemetrycheckpoints";
            eventProcessorHost = new EventProcessorHost(
                    iotHubEventHubName,
                    iotHubTelemetryConsumerGroup,
                    iotHubEventHubEndpointIotHubOwnerConnectionString,
                    solutionStorageAccountConnectionString,
                    StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages.
            EventProcessorOptions options = new EventProcessorOptions();
            options.InitialOffsetProvider = (partitionId) => EventPosition.FromEnqueuedTime(DateTime.UtcNow);
            options.SetExceptionHandler(EventProcessorHostExceptionHandler);
            try
            {
                await eventProcessorHost.RegisterEventProcessorAsync<MessageProcessor>(options);
                Trace.TraceInformation($"EventProcessor successfully registered");
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Exception during register EventProcessorHost '{e.Message}'");
            }

            // Wait till shutdown.
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    Trace.TraceInformation($"Application is shutting down. Unregistering EventProcessorHost...");
                    await eventProcessorHost.UnregisterEventProcessorAsync();
                    return;
                }
                await Task.Delay(1000);
            }
        }

        private static void EventProcessorHostExceptionHandler(ExceptionReceivedEventArgs args)
        {
            Trace.TraceInformation($"EventProcessorHostException: {args.Exception.Message}");
        }
    }
}