
namespace Opc.Ua.Cloud.Dashboard.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Diagnostics;
    using UACloudDashboard.Interfaces;

    public class DashboardController : Controller
    {
        private readonly ISubscriber _subscriber;
        private readonly IMessageProcessor _processor;

        public DashboardController(ISubscriber subscriber, IMessageProcessor processor)
        {
            _subscriber = subscriber;
            _processor = processor;
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        public ActionResult Index()
        {
            return View("Index");
        }

        [HttpPost]
        public IActionResult Display()
        {
            try
            {
                foreach (string key in Request.Form.Keys)
                {
                    if (key.Contains("Siemens"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "siemens/#");
                        break;
                    }

                    if (key.Contains("Trumpf"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "devices/trumpf/#");
                        break;
                    }

                    if (key.Contains("Mettler Toledo"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "devices/mettler/#");
                        break;
                    }

                    if (key.Contains("Matrikon"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Matrikon/#");
                        break;
                    }

                    if (key.Contains("Prosys OPC"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "prosysopc/#");
                        break;
                    }

                    if (key.Contains("Unified Automation"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "unifiedautomation/permanent/temperatures/#");
                        break;
                    }

                    if (key.Contains("Pilz"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "devices/pilz/#");
                        break;
                    }

                    if (key.Contains("Beckhoff"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Test/#");
                        break;
                    }

                    if (key.Contains("Phoenix Contact"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "PhoenixContact/#");
                        break;
                    }

                    if (key.Contains("VDW"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "vdw/json/data/#");
                        break;
                    }

                    if (key.Contains("KUKA"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "KUKA/#");
                        break;
                    }

                    if (key.Contains("Wago"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "oi4/OTConnector/wago.com/#");
                        break;
                    }

                    if (key.Contains("OPC Foundation"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "starterkit/json/data/#");
                        break;
                    }
                }

                _subscriber.Stop();
                _subscriber.Run();

                _processor.Clear();

                return View("Graph");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error: " + ex.Message);
                return View("Index");
            }
        }
    }
}