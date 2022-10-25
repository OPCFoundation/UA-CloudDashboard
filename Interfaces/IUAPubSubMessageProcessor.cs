﻿
namespace UACloudDashboard.Interfaces
{
    using System;

    public interface IUAPubSubMessageProcessor
    {
        void Clear();

        void ProcessMessage(byte[] payload, DateTime receivedTime, string contentType);
    }
}