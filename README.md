# OpcUaWebDashboard
A cloud-based, dockerized dashboard for displaying OPC UA PubSub telemetry data, read directly from an Azure IoT Hub or an MQTT broker. It supports both JSON and binary payloads.

## Installation

The following environment variables need to be defined:

* IotHubEventHubName - the name of your IoT Hub
* EventHubEndpointIotHubOwnerConnectionString - the Event Hub compatible endpoint connection string for the IoT Hub owner (under Built-in endpoints in the Azure portal)
* StorageAccountConnectionString - the connection string of an Azure storage account (under Access keys in the Azure portal)

The following environment variables can be defined:

* USE_MQTT - Read OPC UA PubSub telementry messages from an MQTT Borker instead of Azure IoT Hub
* MQTT_BROKER_NAME - the name of the MQTT broker to use
* MQTT_BROKER_PORT - the port number of the MQTT broker
* MQTT_USE_TLS - set to 1 to use Transport Layer Security with MQTT
* MQTT_CLIENT_NAME - the client name to use with the MQTT broker
* MQTT_USERNAME - the username to use with the MQTT broker
* MQTT_PASSWORD - the password to use with the MQTT broker
* MQTT_TOPIC - the MQTT broker topic to read messages from

Also, within your IoT Hub, you need to define a consumer group (under Built-in endpoints in the Azure portal) called dashboard.

## Usage

It is published on DockerHub: https://hub.docker.com/r/barnstee/opcuawebdashboard

Run it via: docker run -p 80:80 ghcr.io/barnstee/opcuawebdashboard:main

Then point your web browser to <http://localhost>





## Build Status

[![Docker](https://github.com/barnstee/OpcUaWebDashboard/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/barnstee/OpcUaWebDashboard/actions/workflows/docker-publish.yml)

