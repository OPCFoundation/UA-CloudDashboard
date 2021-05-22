using System.Collections.Generic;

namespace OpcUaWebDashboard
{
    public interface ITopologyNode
    {
        List<string> GetChildren();

        void AddChild(ref TopologyNode child);

        string Key { get; set; }
    }
}
