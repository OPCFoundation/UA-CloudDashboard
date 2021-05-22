using Opc.Ua;
using System;
using System.Collections.Generic;

namespace OpcUaWebDashboard.Models
{
    public class DashboardModel
    {
        public string SessionId { get; set; }

        public Type ChildrenType { get; set; }

        public Dictionary<string, DataValue> Children { get; set; }

        public string ChildrenContainerHeader { get; set; }

        public string ChildrenListHeaderStatus { get; set; }

        public string ChildrenListHeaderLocation { get; set; }

        public string ChildrenListHeaderDetails { get; set; }

        public string ShopfloorType { get; set; }
    }
}