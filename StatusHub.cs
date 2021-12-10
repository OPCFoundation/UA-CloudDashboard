
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaWebDashboard
{
    public class StatusHub : Hub
    {
        public Dictionary<string, Tuple<string, string>> TableEntries { get; set; }

        public Dictionary<string, string[]> ChartEntries { get; set; }

        public StatusHub()
        {
            TableEntries = new Dictionary<string, Tuple<string, string>>();
            ChartEntries = new Dictionary<string, string[]>();

            Task.Run(() => SendMessageViaSignalR());
        }

        private async Task SendMessageViaSignalR()
        {
            while (true)
            {
                await Task.Delay(3000).ConfigureAwait(false);

                lock (TableEntries)
                {
                    foreach (string displayName in TableEntries.Keys)
                    {
                        Clients?.All.SendAsync("addDatasetToChart", displayName).GetAwaiter().GetResult();
                    }

                    foreach (KeyValuePair<string, string[]> entry in ChartEntries)
                    {
                        Clients?.All.SendAsync("addDataToChart", entry.Key, entry.Value).GetAwaiter().GetResult();
                    }
                    ChartEntries.Clear();

                    CreateTableForTelemetry();
                }
            }
        }

        private void CreateTableForTelemetry()
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
            foreach (KeyValuePair<string, Tuple<string, string>> item in TableEntries)
            {
                sb.Append("<tr>");
                sb.Append("<td style='width:400px'>" + item.Key + "</td>");
                sb.Append("<td style='width:200px'>" + item.Value.Item1 + "</td>");
                sb.Append("<td style='width:200px'>" + item.Value.Item2 + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");

            Clients?.All.SendAsync("addTable", sb.ToString()).GetAwaiter().GetResult();
        }
    }
}
