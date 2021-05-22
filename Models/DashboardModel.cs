﻿using System;
using System.Collections.Generic;

namespace OpcUaWebDashboard.Models
{
    /// <summary>
    /// A view model for the Dashboard view.
    /// </summary>
    public class DashboardModel
    {
        /// <summary>
        /// The ID of the active session.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The node in the topology the view is visualising.
        /// </summary>
        public ContosoTopologyNode TopNode { get; set; }

        public Type ChildrenType { get; set; }

        public List<ContosoChildInfo> Children;


        /// <summary>
        /// The bing maps license key.
        /// </summary>
        public string MapApiQueryKey { get; set; }

        /// <summary>
        /// Header text of the containment of the children list of the current view.
        /// </summary>
        public string ChildrenContainerHeader { get; set; }

        /// <summary>
        /// List header text of the status column.
        /// </summary>
        public string ChildrenListHeaderStatus { get; set; }

        /// <summary>
        /// List header text of the location column.
        /// </summary>
        public string ChildrenListHeaderLocation { get; set; }

        /// <summary>
        /// List header text of the details column.
        /// </summary>
        public string ChildrenListHeaderDetails { get; set; }

        /// <summary>
        /// The shopfloor type of the current view.
        /// </summary>
        public string ShopfloorType;

        /// <summary>
        /// Initializes a new instance of the DashboardModel class.
        /// </summary>
        public DashboardModel()
        {
            Children = new List<ContosoChildInfo>();
        }
    }
}