
namespace OpcUaWebDashboard
{
    using System;

    public interface IUAPubSubMessageProcessor
    {
        void ProcessMessage(byte[] payload, DateTime receivedTime);
    }
}