using OpcUaWebDashboard.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    public class StatusHub : Hub
    {
    }

    /// <summary>
    /// A Controller for Dashboard-related views.
    /// </summary>
    public class DashboardController : Controller
    {
        public static void RemoveSessionFromSessionsViewingStations(string sessionId)
        {
            _sessionListSemaphore.Wait();
            try
            {
                _sessionsViewingStations.Remove(sessionId);
            }
            finally
            {
                _sessionListSemaphore.Release();
            }
        }

        public static int SessionsViewingStationsCount()
        {
            int count = 0;
            _sessionListSemaphore.Wait();
            try
            {
                count = _sessionsViewingStations.Count;
            }
            finally
            {
                _sessionListSemaphore.Release();
            }
            return count;
        }

        public static void Init()
        {
            if (_sessionListSemaphore == null)
            {
                _sessionListSemaphore = new SemaphoreSlim(1);
            }
            if (_sessionsViewingStations == null)
            {
                _sessionsViewingStations = new HashSet<string>();
            }
        }

        public static void Deinit()
        {
            if (_sessionListSemaphore != null)
            {
                _sessionListSemaphore.Dispose();
                _sessionListSemaphore = null;
            }
            if (_sessionsViewingStations != null)
            {
                _sessionsViewingStations = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the DashboardController class.
        /// </summary>
        public DashboardController()
        {
        }

        public ActionResult Index(string topNode)
        {
            DashboardModel dashboardModel;

            _sessionListSemaphore.Wait();

            try
            {
                // Reset topNode if invalid.
                if (string.IsNullOrEmpty(topNode) || ContosoTopology.Topology[topNode] == null)
                {
                    topNode = ContosoTopology.Topology.TopologyRoot.Key;
                }

                if (HttpContext.Session != null && HttpContext.Session.Id != null && ContosoTopology.SessionList.ContainsKey(HttpContext.Session.Id))
                {
                    // The session is known.
                    Trace.TraceInformation($"Session '{HttpContext.Session.Id}' is known");
                    dashboardModel = ContosoTopology.SessionList[HttpContext.Session.Id];

                    // Set the new dashboard TopNode.
                    dashboardModel.TopNode = (ContosoTopologyNode)ContosoTopology.Topology[topNode];

                    // Update type of children for the view and track sessions viewing at stations
                    Type topNodeType = dashboardModel.TopNode.GetType();
                    if (topNodeType == typeof(Station))
                    {
                        _sessionsViewingStations.Add(HttpContext.Session.Id);
                        dashboardModel.ChildrenType = typeof(ContosoOpcUaNode);
                    }
                    else
                    {
                        // We track all sessions viewing stations in a list, to optimize client session updates.
                        _sessionsViewingStations.Remove(HttpContext.Session.Id);

                        // Set the children type. We only allow children of similar type.
                        if (dashboardModel.TopNode.GetChildren().Count > 0)
                        {
                            var key = dashboardModel.TopNode.GetChildren()[0];
                            dashboardModel.ChildrenType = ContosoTopology.Topology[key].GetType();
                        }
                        else
                        {
                            // We must be at root and detected a new station without nodes
                            _sessionsViewingStations.Add(HttpContext.Session.Id);
                            dashboardModel.ChildrenType = typeof(ContosoOpcUaNode);
                        }
                    }
                    Trace.TraceInformation($"{_sessionsViewingStations.Count} session(s) viewing at Station nodes");
                }
                else
                {
                    // Create a new model and add it to the session list.
                    dashboardModel = new DashboardModel();
                    dashboardModel.TopNode = (ContosoTopologyNode)ContosoTopology.Topology.TopologyRoot;
                    dashboardModel.SessionId = HttpContext.Session.Id;
                    dashboardModel.ChildrenType = typeof(Factory);

                    Trace.TraceInformation($"Add new session '{HttpContext.Session.Id}' to session list");
                    ContosoTopology.SessionList.Add(HttpContext.Session.Id, dashboardModel);

                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"Exception in DashboardController ({e.Message})");
                dashboardModel = ContosoTopology.SessionList[HttpContext.Session.Id];
                dashboardModel.TopNode = (ContosoTopologyNode)ContosoTopology.Topology.TopologyRoot;
                dashboardModel.ChildrenType = typeof(Factory);
                dashboardModel.SessionId = HttpContext.Session.Id;
                _sessionsViewingStations.Remove(HttpContext.Session.Id);
            }
            finally
            {
                _sessionListSemaphore.Release();
            }

            // Set shopfloor type
            dashboardModel.ShopfloorType = dashboardModel.TopNode.ShopfloorType;

            // Update the children info.
            Trace.TraceInformation($"Show dashboard view for ({dashboardModel.TopNode.Key})");
            dashboardModel.Children = ContosoTopology.Topology.GetChildrenInfo(dashboardModel.TopNode.Key);
            if (dashboardModel.ChildrenType == typeof(Factory))
            {
                dashboardModel.ChildrenContainerHeader = Resources.ChildrenFactoryListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Resources.ChildrenFactoryListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Resources.ChildrenFactoryListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Resources.ChildrenFactoryListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(ProductionLine))
            {
                dashboardModel.ChildrenContainerHeader = dashboardModel.TopNode.Name + " " + Resources.ChildrenProductionLineListContainerHeaderPostfix;
                dashboardModel.ChildrenListHeaderDetails = Resources.ChildrenProductionLineListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Resources.ChildrenProductionLineListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Resources.ChildrenProductionLineListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(Station))
            {
                if ((dashboardModel.TopNode.Key == "TopologyRoot") || (dashboardModel.TopNode.Key == "topologyroot"))
                {
                    dashboardModel.ChildrenContainerHeader = Resources.ChildrenStationListContainerHeaderPostfix;
                }
                else if ((dashboardModel.TopNode.Parent == "TopologyRoot") || (dashboardModel.TopNode.Parent == "topologyroot"))
                {
                    dashboardModel.ChildrenContainerHeader = dashboardModel.TopNode.Name + " " + Resources.ChildrenStationListContainerHeaderPostfix;
                }
                else
                {
                    dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)ContosoTopology.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Resources.ChildrenStationListContainerHeaderPostfix;
                }
                dashboardModel.ChildrenListHeaderDetails = Resources.ChildrenStationListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Resources.ChildrenStationListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Resources.ChildrenStationListListHeaderStatus;
            }
            if (dashboardModel.ChildrenType == typeof(ContosoOpcUaNode))
            {
                if ((dashboardModel.TopNode.Parent == "TopologyRoot") || (dashboardModel.TopNode.Parent == "topologyroot"))
                {
                    dashboardModel.ChildrenContainerHeader = dashboardModel.TopNode.Name + " " + Resources.ChildrenOpcUaNodeListContainerHeaderPostfix;
                }
                else
                {
                    dashboardModel.ChildrenContainerHeader = ((ContosoTopologyNode)ContosoTopology.Topology[ContosoTopology.Topology[dashboardModel.TopNode.Parent].Parent]).Name + " - " + ((ContosoTopologyNode)ContosoTopology.Topology[dashboardModel.TopNode.Parent]).Name + " - " + dashboardModel.TopNode.Name + " " + Resources.ChildrenOpcUaNodeListContainerHeaderPostfix;
                }
                dashboardModel.ChildrenListHeaderDetails = Resources.ChildrenOpcUaNodeListListHeaderDetails;
                dashboardModel.ChildrenListHeaderLocation = Resources.ChildrenOpcUaNodeListListHeaderLocation;
                dashboardModel.ChildrenListHeaderStatus = Resources.ChildrenOpcUaNodeListListHeaderStatus;
            }
            return View(dashboardModel);
        }

        private static SemaphoreSlim _sessionListSemaphore = null;
        private static HashSet<string>_sessionsViewingStations;
    }
}