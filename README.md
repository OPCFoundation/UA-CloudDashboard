# OpcUaWebDashboard
A cloud-based, dockerized dashboard for displaying OPC UA PubSub telemetry data, read directly from an Azure IoT Hub.

## Installation

The following environment variables need to be defined:

* IotHubEventHubName - the name of your IoT Hub
* EventHubEndpointIotHubOwnerConnectionString - the Event Hub compatible endpoint connection string for the IoT Hub owner (under Built-in endpoints in the Azure portal)
* StorageAccountConnectionString - the connection string of an Azure storage account (under Access keys in the Azure portal)

Also, within your IoT Hub, you need to define a consumer group (under Built-in endpoints in the Azure portal) called dashboard.

## Usage

It is published on DockerHub: https://hub.docker.com/r/barnstee/opcuawebdashboard

Run it via: docker run -p 80:80 ghcr.io/barnstee/opcuawebdashboard:main

Then point your web browser to <http://localhost>

## Build Status

[![Docker](https://github.com/barnstee/OpcUaWebDashboard/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/barnstee/OpcUaWebDashboard/actions/workflows/docker-publish.yml)

