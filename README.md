# UA Cloud Dashboard
A cross-platform OPC UA cloud dashboard reference implementation leveraging Kafka and MQTT. It runs in a Docker container and displays OPC UA PubSub telemetry data, read directly from a Kafka broker or an MQTT broker. It supports both JSON and binary payloads as well as OPC UA Complex Types decoding.

## Installation

The following environment variables must be defined:

* BROKER_NAME - the name of the broker to use
* BROKER_PORT - the port number of the broker
* CLIENT_NAME - the client name to use with the broker
* BROKER_USERNAME - the username to use with the broker
* BROKER_PASSWORD - the password to use with the broker
* TOPIC - the broker topic to read messages from
* METADATA_TOPIC - (optional) the broker metadata topic to read messages from
* USE_MQTT - (optional) Read OPC UA PubSub telementry messages from an MQTT borker instead of a Kafka broker
* USE_TLS - (optional) set to 1 to use Transport Layer Security
* IGNORE_MISSING_METADATA - (optional) set to 1 to parse messages even if no metadata was sent for the messages

## Usage

Run it on a Docker-enabled computer via:

`docker run -e anEnvironmentVariableFromAbove="yourSetting" -p 80:8080 ghcr.io/barnstee/ua-clouddashboard:main`

Then point your web browser to <http://yourIPAddress>

## Build Status

[![Docker](https://github.com/barnstee/UA-CloudDashboard/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/barnstee/UA-CloudDashboard/actions/workflows/docker-publish.yml)

