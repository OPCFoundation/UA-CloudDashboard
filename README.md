# OpcUaWebDashboard
A cloud-based, dockerized dashboard for displaying OPC UA PubSub telemetry data, read directly from an Azure IoT Hub.

The following environment variables need to be defined:

* IotHubEventHubName - the name of your IoT Hub
* EventHubEndpointIotHubOwnerConnectionString - the Event Hub compatible endpoint connection string for the IoT Hub owner (under Built-in endpoints in the Azure portal)
* StorageAccountConnectionString - the connection string of an Azure storage account (under Access keys in the Azure portal)

Also, within your IoT Hub, you need to define a consumer group (under Built-in endpoints in the Azure portal) called dashboard.
