using Opc.Ua;
using System;
using System.Collections.Generic;

namespace OpcUaWebDashboard.Models
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

        public DateTime Timestamp { get; set; }

        public Dictionary<string, Value> Payload { get; set; }
    }

    public class MetaDataVersion
    {
        public int MajorVersion { get; set; }

        public int MinorVersion { get; set; }
    }

    public class Value
    {
        public string value { get; set; }
        
        public string Body { get; set; }

        public int Type { get; set; }
                
        public Value(string body, int type)
        {
            Body = body;
            Type = type;
        }

    }
}
