
namespace Opc.Ua.Cloud.Dashboard
{
    using Microsoft.AspNetCore.SignalR;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class StatusHub : Hub
    {
        // this is our SignalR Status Hub
    }

    public class StatusHubClient
    {
        public Dictionary<string, Tuple<string, string>> TableEntries { get; set; } = new Dictionary<string, Tuple<string, string>>();

        public Dictionary<string, string[]> ChartEntries { get; set; } = new Dictionary<string, string[]>();

        private readonly IHubContext<StatusHub> _hubContext;

        public StatusHubClient(IHubContext<StatusHub> hubContext)
        {
            _hubContext = hubContext;

            _ = Task.Run(() => SendMessageViaSignalR());
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
                        _hubContext.Clients.All.SendAsync("addDatasetToChart", displayName).GetAwaiter().GetResult();
                    }

                    foreach (KeyValuePair<string, string[]> entry in ChartEntries)
                    {
                        _hubContext.Clients.All.SendAsync("addDataToChart", entry.Key, entry.Value).GetAwaiter().GetResult();
                    }

                    ChartEntries.Clear();

                    CreateAndSendTelemetryTable();
                }
            }
        }

        private void CreateAndSendTelemetryTable()
        {
            // create HTML table
            StringBuilder sb = new StringBuilder();
            sb.Append("<table class='table table-hover telemetry-table mb-0'>");

            // header
            sb.Append("<thead><tr>");
            sb.Append("<th scope='col'>Name</th>");
            sb.Append("<th scope='col'>Latest Value</th>");
            sb.Append("<th scope='col'>Time Stamp</th>");
            sb.Append("</tr></thead>");

            // rows
            sb.Append("<tbody>");
            foreach (KeyValuePair<string, Tuple<string, string>> item in TableEntries.ToImmutableSortedDictionary())
            {
                sb.Append("<tr>");
                sb.Append("<td class='telemetry-name'>" + WebUtility.HtmlEncode(item.Key) + "</td>");
                sb.Append("<td class='telemetry-value'>" + WebUtility.HtmlEncode(item.Value.Item1) + "</td>");
                sb.Append("<td class='telemetry-time'>" + WebUtility.HtmlEncode(item.Value.Item2) + "</td>");
                sb.Append("</tr>");
            }

            if (TableEntries.Count == 0)
            {
                sb.Append("<tr><td colspan='3' class='table-placeholder'>Waiting for telemetry data...</td></tr>");
            }

            sb.Append("</tbody></table>");

            _hubContext.Clients.All.SendAsync("addTable", sb.ToString()).GetAwaiter().GetResult();
        }
    }
}
