﻿
namespace Opc.Ua.Cloud.Dashboard
{
    using Confluent.Kafka;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;
    using UACloudDashboard.Interfaces;

    public class KafkaSubscriber : ISubscriber
    {
        private IConsumer<Ignore, byte[]> _consumer;
        private readonly IUAPubSubMessageProcessor _uaMessageProcessor;

        public KafkaSubscriber(IUAPubSubMessageProcessor uaMessageProcessor)
        {
            _uaMessageProcessor = uaMessageProcessor;
        }

        public void Connect()
        {
            try
            {
                // disconnect if still connected
                if (_consumer != null)
                {
                    _consumer.Close();
                    _consumer.Dispose();
                    _consumer = null;
                }

                // create Kafka client
                var conf = new ConsumerConfig
                {
                    GroupId = "consumer-group",
                    BootstrapServers = Environment.GetEnvironmentVariable("BROKER_NAME") + ":" + Environment.GetEnvironmentVariable("BROKER_PORT"),
                    // Note: The AutoOffsetReset property determines the start offset in the event
                    // there are not yet any committed offsets for the consumer group for the
                    // topic/partitions of interest. By default, offsets are committed
                    // automatically, so in this example, consumption will only start from the
                    // earliest message in the topic 'my-topic' the first time you run the program.
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = Environment.GetEnvironmentVariable("BROKER_USERNAME"),
                    SaslPassword = Environment.GetEnvironmentVariable("BROKER_PASSWORD")
                };

                _consumer = new ConsumerBuilder<Ignore, byte[]>(conf).Build();

                _consumer.Subscribe(Environment.GetEnvironmentVariable("TOPIC"));

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("METADATA_TOPIC")))
                {
                    _consumer.Subscribe(new List<string>() {
                        Environment.GetEnvironmentVariable("TOPIC"),
                        Environment.GetEnvironmentVariable("METADATA_TOPIC")
                    });
                }

                Trace.TraceInformation("Connected to Kafka broker.");

                _ = Task.Run(() =>
                {
                    while (true)
                    {
                        ConsumeResult<Ignore, byte[]> result = _consumer.Consume();

                        if (result.Message != null)
                        {
                            string contentType = "application/json";
                            if (result.Message.Headers != null && result.Message.Headers.Count > 0)
                            {
                                foreach (var header in result.Message.Headers)
                                {
                                    if (header.Key.Equals("Content-Type"))
                                    {
                                        contentType = Encoding.UTF8.GetString(header.GetValueBytes());
                                    }
                                }
                            }

                            _uaMessageProcessor.ProcessMessage(result.Message.Value, result.Message.Timestamp.UtcDateTime, contentType);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }
    }
}