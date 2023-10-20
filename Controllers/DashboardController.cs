
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

                    if (key.Contains("2mag"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "2mag/#");
                        break;
                    }

                    if (key.Contains("Agilent"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Agilent/#");
                        break;
                    }

                    if (key.Contains("Byonoy"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "byonoy/#");
                        break;
                    }

                    if (key.Contains("FHI"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "FHI/#");
                        break;
                    }

                    if (key.Contains("Gambica"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Gambica/#");
                        break;
                    }

                    if (key.Contains("Infoteam"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Infoteam/#");
                        break;
                    }

                    if (key.Contains("Integris"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Integris/#");
                        break;
                    }

                    if (key.Contains("Julabo"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Julabo/#");
                        break;
                    }

                    if (key.Contains("Alresa"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Alresa/#");
                        break;
                    }

                    if (key.Contains("Spectaris"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "SPECTARIS/#");
                        break;
                    }

                    if (key.Contains("Amensio"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Amensio/#");
                        break;
                    }

                    if (key.Contains("Brand"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Brand/#");
                        break;
                    }

                    if (key.Contains("Essentim"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Essentim/#");
                        break;
                    }

                    if (key.Contains("Labforward"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Labforward/#");
                        break;
                    }

                    if (key.Contains("InforsHT"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "InforsHT/#");
                        break;
                    }

                    if (key.Contains("Labmas"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Labmas/#");
                        break;
                    }

                    if (key.Contains("Lads"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Lads/#");
                        break;
                    }

                    if (key.Contains("Jaima"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Jaima/#");
                        break;
                    }

                    if (key.Contains("Berthold"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "berthold/#");
                        break;
                    }

                    if (key.Contains("Gefran"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Gefran/#");
                        break;
                    }

                    if (key.Contains("Microsoft"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Microsoft/#");
                        break;
                    }

                    if (key.Contains("Schneider"))
                    {
                        Environment.SetEnvironmentVariable("TOPIC", "Schneider-Electric/#");
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