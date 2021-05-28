using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
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

        public DashboardController(IHubContext<StatusHub> hubContext)
        {
            _hubContext = hubContext;
            _latestTelemetry = new List<Tuple<string, string, string>>();
        }

        public static void AddDatasetToChart(string name)
        {
            _hubContext.Clients.All.SendAsync("addDatasetToChart", name).GetAwaiter().GetResult();
        }

        public static void AddDataToChart(string timestamp, float[] values)
        {
            _hubContext.Clients.All.SendAsync("addDataToChart", timestamp, values).GetAwaiter().GetResult();
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        public ActionResult Index()
        {
            return View("Index");
        }

        public static void CreateTableForTelemetry(List<Tuple<string,string, string>> telemetry)
        {
            bool found = false;
            foreach (Tuple<string, string, string> item in telemetry)
            {
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
            
            // create HTML table
            StringBuilder sb = new StringBuilder();
            sb.Append("<table width='600px' cellpadding='3' cellspacing='3'>");
            
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
                sb.Append("<td style='width:200px'>" + item.Item1 + "</td>");
                sb.Append("<td style='width:200px'>" + item.Item2 + "</td>");
                sb.Append("<td style='width:200px'>" + item.Item3 + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            
            _hubContext.Clients.All.SendAsync("addTable", sb.ToString()).GetAwaiter().GetResult();
        }
    }
}