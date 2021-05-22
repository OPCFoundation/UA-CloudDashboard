using System;
using System.Collections.Generic;

namespace OpcUaWebDashboard
{
    public interface ITopologyTree
    {
        TopologyNode TopologyRoot { get; }

        void AddChild(string key, TopologyNode child);

        List<string> GetAllChildren(string key);

        List<string> GetAllChildren(string key, Type type);
    }
}
