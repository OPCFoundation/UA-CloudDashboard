using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace OpcUaWebDashboard.Controllers
{
    public class StatusHub : Hub
    {
    }

    public class DashboardController : Controller
    {
        private static IHubContext<StatusHub> _hubContext;

        public DashboardController(IHubContext<StatusHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public static void AddDatasetToChart(string name)
        {
            _hubContext.Clients.All.SendAsync("addDatasetToChart", name).Wait();
        }
        public static void AddDataToChart(string dataset, string label, float value)
        {
            _hubContext.Clients.All.SendAsync("addDataToChart", dataset, label, value).Wait();
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