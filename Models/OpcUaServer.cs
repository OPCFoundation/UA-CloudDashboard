using System.Collections.Generic;

namespace OpcUaWebDashboard
{
    /// <summary>
    /// Class for a OPC UA node in an OPC UA server.
    /// </summary>
    public class OpcUaNode
    {
        /// <summary>
        /// The OPC UA node Id of this OPC UA node.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// The OPC UA symbolic name for this OPC UA node.
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// Ctor for the OPC UA node..
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="symbolicName"></param>
        public OpcUaNode(string nodeId, string symbolicName)
        {
            NodeId = nodeId;
            SymbolicName = symbolicName;
        }
    }

    /// <summary>
    /// Represents an OPC UA server. It owns all OPC UA entities (like OPC UA NodeId's, ...) exposed by the server.
    /// </summary>
    public class OpcUaServer
    {
        /// <summary>
        /// The list of OPC UA nodes in this OPC UA server.
        /// </summary>
        public List<OpcUaNode> NodeList;

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        public string EndpointUrl;

        /// <summary>
        /// Controls if a secure connection should be established to the OPC UA server.
        /// </summary>
        public bool UseSecurity;

        /// <summary>
        /// The OPC UA application URI of the server.
        /// </summary>
        public string ApplicationUri;

        /// <summary>
        /// Creates a new topology node, which is an OPC UA server. A station in the topology is equal to an OPC UA server.
        /// This is the reason why the station description is passed in as a parameter.
        /// </summary>
        public OpcUaServer(string shopfloorDomain, string shopfloorType, string uri, string url, string name, string description, bool useSecurity)
        {
            NodeList = new List<OpcUaNode>();
            EndpointUrl = url;
            UseSecurity = useSecurity;
            ApplicationUri = uri + (string.IsNullOrEmpty(shopfloorDomain) ? "" : (":" + shopfloorDomain));
        }

        /// <summary>
        /// Adds an OPC UA node to this OPC UA server.
        /// </summary>
        public void AddOpcUaServerNode(string opcUaNodeId, string opcUaSymbolicName)
        {
            OpcUaNode opcNodeObject = new OpcUaNode(opcUaNodeId, opcUaSymbolicName);
            NodeList.Add(opcNodeObject);
        }
        /// <summary>
        /// Checks if the OPC UA server has an OPC UA node with the given nodeId.
        /// </summary>
        public OpcUaNode GetOpcUaNode(string opcUaNodeId)
        {
            foreach (OpcUaNode opcUaNode in NodeList)
            {
                if (opcUaNode.NodeId == opcUaNodeId)
                {
                    return opcUaNode;
                }
            }
            return null;
        }
    }
}
