using Microsoft.AspNetCore.Mvc;

namespace OpcUaWebDashboard.Controllers
{
    public class DashboardController : Controller
    {
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