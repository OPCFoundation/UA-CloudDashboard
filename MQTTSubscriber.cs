
namespace OpcUaWebDashboard
{
    using MQTTnet;
    using MQTTnet.Adapter;
    using MQTTnet.Client;
    using MQTTnet.Client.Connecting;
    using MQTTnet.Client.Options;
    using MQTTnet.Client.Subscribing;
    using MQTTnet.Packets;
    using MQTTnet.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class MQTTSubscriber
    {
        private static IMqttClient _client = null;
        private static IUAPubSubMessageProcessor _uaMessageProcessor;

        public static void Connect()
        {
            try
            {
                _uaMessageProcessor = (IUAPubSubMessageProcessor)Program.AppHost.Services.GetService(typeof(IUAPubSubMessageProcessor));

                // create MQTT client
                string password = Environment.GetEnvironmentVariable("MQTT_PASSWORD");
                _client = new MqttFactory().CreateMqttClient();
                _client.UseApplicationMessageReceivedHandler(msg => HandleMessageAsync(msg));
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(opt => opt.NoDelay = true)
                    .WithClientId(Environment.GetEnvironmentVariable("MQTT_CLIENT_NAME"))
                    .WithTcpServer(Environment.GetEnvironmentVariable("MQTT_BROKER_NAME"), 8883)
                    .WithTls(new MqttClientOptionsBuilderTlsParameters { UseTls = true })
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(30))
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(300))
                    .WithCleanSession(false) // keep existing subscriptions 
                    .WithCredentials(Environment.GetEnvironmentVariable("MQTT_USERNAME"), password);

                // setup disconnection handling
                _client.UseDisconnectedHandler(disconnectArgs =>
                {
                    Trace.TraceInformation($"Disconnected from MQTT broker: {disconnectArgs.Reason}");

                // simply reconnect again
                Connect();
                });

                try
                {
                    var connectResult = _client.ConnectAsync(clientOptions.Build(), CancellationToken.None).GetAwaiter().GetResult();
                    if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
                    {
                        var status = GetStatus(connectResult.UserProperties)?.ToString("x4");
                        throw new Exception($"Connection to MQTT broker failed. Status: {connectResult.ResultCode}; status: {status}");
                    }

                    var subscribeResult = _client.SubscribeAsync(
                        new MqttTopicFilter
                        {
                            Topic = Environment.GetEnvironmentVariable("MQTT_TOPIC"),
                            QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
                        }).GetAwaiter().GetResult();

                    // make sure subscriptions were successful
                    if (subscribeResult.Items.Count != 1 || subscribeResult.Items[0].ResultCode != MqttClientSubscribeResultCode.GrantedQoS0)
                    {
                        throw new ApplicationException("Failed to subscribe");
                    }
                }
                catch (MqttConnectingFailedException ex)
                {
                    Trace.TraceError($"Failed to connect with reason {ex.ResultCode} and message: {ex.Message}");
                    if (ex.Result?.UserProperties != null)
                    {
                        foreach (var prop in ex.Result.UserProperties)
                        {
                            Trace.TraceError($"{prop.Name}: {prop.Value}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to connect to MQTT broker: " + ex.Message);
            }
        }

        private static MqttApplicationMessage BuildResponse(string status, string id, string responseTopic, byte[] payload)
        {
            return new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithTopic($"{responseTopic}/{status}/{id}")
                .WithPayload(payload)
                .Build();
        }

        // parses status from packet properties
        private static int? GetStatus(List<MqttUserProperty> properties)
        {
            var status = properties.FirstOrDefault(up => up.Name == "status");
            if (status == null)
            {
                return null;
            }

            return int.Parse(status.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        // handles all incoming messages
        private static async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            Trace.TraceInformation($"Received method call with topic: {args.ApplicationMessage.Topic} and payload: {args.ApplicationMessage.ConvertPayloadToString()}");

            string requestTopic = Environment.GetEnvironmentVariable("MQTT_TOPIC");
            string requestID = args.ApplicationMessage.Topic.Substring(args.ApplicationMessage.Topic.IndexOf("?"));

            try
            {
                string requestPayload = args.ApplicationMessage.ConvertPayloadToString();
                byte[] responsePayload = null;

                // route this to the right handler
                if (args.ApplicationMessage.Topic.StartsWith(requestTopic.TrimEnd('#') + "Data"))
                {
                    _uaMessageProcessor.ProcessMessage(args.ApplicationMessage.Payload, DateTime.UtcNow);
                    responsePayload = Encoding.UTF8.GetBytes("Success");
                }
           
                else
                {
                    Trace.TraceError("Unknown command received: " + args.ApplicationMessage.Topic);
                }

                // send reponse to MQTT broker
                //await _client.PublishAsync(BuildResponse("200", requestID, args.ApplicationMessage.ResponseTopic, responsePayload)).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);

                // send error to MQTT broker
                await _client.PublishAsync(BuildResponse("500", requestID, args.ApplicationMessage.ResponseTopic, Encoding.UTF8.GetBytes(ex.Message))).ConfigureAwait(false);
            }
        }
    }
}