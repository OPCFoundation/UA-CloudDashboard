using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpcUaWebDashboard.Models;
using OpcUaWebDashboard.Properties;
using System;

namespace OpcUaWebDashboard.Controllers
{
    public class StatusHub : Hub
    {
    }

    /// <summary>
    /// A Controller for Dashboard-related views.
    /// </summary>
    public class DashboardController : Controller
    {
        private IHubContext<StatusHub> _hubContext;

        /// <summary>
        /// Initializes a new instance of the DashboardController class.
        /// </summary>
        public DashboardController(IHubContext<StatusHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Sends the message to all connected clients as status indication
        /// </summary>
        /// <param name="message">Text to show on web page</param>
        private void UpdateStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(nameof(message));
            }

            _hubContext.Clients.All.SendAsync("addNewMessageToPage", HttpContext?.Session.Id, message).Wait();
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        public ActionResult Index()
        {
            DashboardModel dashboardModel = new DashboardModel();
            dashboardModel.ChildrenType = typeof(ContosoOpcUaNode);
            dashboardModel.SessionId = HttpContext.Session.Id;
            dashboardModel.ShopfloorType = "Simulation";
            dashboardModel.Children = MessageProcessor.ReceivedDataValues;
            dashboardModel.ChildrenContainerHeader = Resources.ChildrenOpcUaNodeListContainerHeaderPostfix;
            dashboardModel.ChildrenListHeaderDetails = Resources.ChildrenOpcUaNodeListListHeaderDetails;
            dashboardModel.ChildrenListHeaderLocation = Resources.ChildrenOpcUaNodeListListHeaderLocation;
            dashboardModel.ChildrenListHeaderStatus = Resources.ChildrenOpcUaNodeListListHeaderStatus;
            return View(dashboardModel);
        }
    }
}