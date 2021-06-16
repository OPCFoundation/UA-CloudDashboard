using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OpcUaWebDashboard.Controllers
{
    public class StatusHub : Hub
    {
    }

    public class DashboardController : Controller
    {
        private static IHubContext<StatusHub> _hubContext;
        private static List<Tuple<string, string, string>> _latestTelemetry;
        private static Stopwatch _tableTimer;
        private static bool _pageReloaded = false;

        public DashboardController(IHubContext<StatusHub> hubContext)
        {
            _hubContext = hubContext;
            _latestTelemetry = new List<Tuple<string, string, string>>();
            _tableTimer = new Stopwatch();
            _tableTimer.Start();
        }

        public static void AddDatasetToChart(string name)
        {
            _hubContext.Clients.All.SendAsync("addDatasetToChart", name).GetAwaiter().GetResult();
            Debug.WriteLine("Sent new data set: " + name);
        }

        public static void AddDataToChart(string timestamp, string[] values)
        {
            _hubContext.Clients.All.SendAsync("addDataToChart", timestamp, values).GetAwaiter().GetResult();
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        public ActionResult Index()
        {
            _pageReloaded = true;

            return View("Index");
        }

        public static void CreateTableForTelemetry(List<Tuple<string,string, string>> telemetry)
        {
            foreach (Tuple<string, string, string> item in telemetry)
            {
                bool found = false;
                for (int i = 0; i < _latestTelemetry.Count; i++)
                {
                    if (_latestTelemetry[i].Item1 == item.Item1)
                    {
                        _latestTelemetry[i] = item;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _latestTelemetry.Add(item);
                }
            }

            // update table every second
            if (_tableTimer.ElapsedMilliseconds > 1000)
            {
                // create HTML table
                StringBuilder sb = new StringBuilder();
                sb.Append("<table width='800px' cellpadding='3' cellspacing='3'>");

                // header
                sb.Append("<tr>");
                sb.Append("<th><b>OPC UA Node ID</b></th>");
                sb.Append("<th><b>Latest Value</b></th>");
                sb.Append("<th><b>Time Stamp</b></th>");
                sb.Append("</tr>");

                // rows
                foreach (Tuple<string, string, string> item in _latestTelemetry)
                {
                    sb.Append("<tr>");
                    sb.Append("<td style='width:400px'>" + item.Item1 + "</td>");
                    sb.Append("<td style='width:200px'>" + item.Item2 + "</td>");
                    sb.Append("<td style='width:200px'>" + item.Item3 + "</td>");
                    sb.Append("</tr>");
                }

                sb.Append("</table>");

                _hubContext.Clients.All.SendAsync("addTable", sb.ToString()).GetAwaiter().GetResult();

                // if the page was reloaded in the mean-time, resend the data sets
                if (_pageReloaded == true)
                {
                    foreach (string nodeId in MessageProcessor.NodeIDs)
                    {
                        AddDatasetToChart(nodeId);
                    }

                    _pageReloaded = false;
                }

                _tableTimer.Restart();
            }
        }
    }
}