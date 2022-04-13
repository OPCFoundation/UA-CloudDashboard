
namespace Opc.Ua.Cloud.Dashboard.Controllers
{
    using Microsoft.AspNetCore.Mvc;

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