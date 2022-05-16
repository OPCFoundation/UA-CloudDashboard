
namespace Opc.Ua.Cloud.Dashboard.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Diagnostics;

    public class DashboardController : Controller
    {
        private readonly IUAPubSubMessageProcessor _uaMessageProcessor;

        public DashboardController(IUAPubSubMessageProcessor processor)
        {
            _uaMessageProcessor = processor;
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
                        Environment.SetEnvironmentVariable("MQTT_TOPIC", "siemens/#");
                        break;
                    }

                    if (key.Contains("Trumpf"))
                    {
                        Environment.SetEnvironmentVariable("MQTT_TOPIC", "devices/trumpf/#");
                        break;
                    }

                    if (key.Contains("Mettler Toledo"))
                    {
                        Environment.SetEnvironmentVariable("MQTT_TOPIC", "devices/mettler/#");
                        break;
                    }

                    if (key.Contains("Matrikon"))
                    {
                        Environment.SetEnvironmentVariable("MQTT_TOPIC", "Mtkon/#");
                        break;
                    }

                    if (key.Contains("Prosys OPC"))
                    {
                        Environment.SetEnvironmentVariable("MQTT_TOPIC", "prosysopc/#");
                        break;
                    }

                    if (key.Contains("Unified Automation"))
                    {
                        Environment.SetEnvironmentVariable("MQTT_TOPIC", "unifiedautomation/#");
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("USE_MQTT")))
                {
                    MQTTSubscriber.Connect();
                }
                else
                {
                    IoTHubConfig.Connect();
                }

                _uaMessageProcessor.Clear();

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