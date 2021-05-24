using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
        private static IHubContext<StatusHub> _hubContext;

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
        public static void UpdateStatus(string label, float value)
        {
            _hubContext.Clients.All.SendAsync("addNewMessageToPage", label, value).Wait();
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        public ActionResult Index()
        {
            return View("Index");
        }
    }
}