using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp;
using Microsoft.Extensions.Hosting;

namespace OpcUaWebDashboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IoTHubConfig.ConfigureIotHub();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
