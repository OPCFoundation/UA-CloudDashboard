using System;

namespace OpcUaWebDashboard.Models
{
    public class SignalRModel
    {
        public string NodeID { get; set; }

        public float Value { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}
