
namespace Opc.Ua.Cloud.Dashboard
{
    using System;

    public interface IUAPubSubMessageProcessor
    {
        void ProcessMessage(byte[] payload, DateTime receivedTime, string contentType);
    }
}