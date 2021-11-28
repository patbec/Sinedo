using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Logging;
using Sinedo.Singleton;

namespace Sinedo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            _ = Configuration.Current; // Throw if config is invalid.

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureLogging(logBuilder =>
                {
                    logBuilder.AddProvider(WebViewLoggerProvider.Default);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(listenOptions =>
                    {
                        listenOptions.Listen(IPAddress.Parse(Configuration.Current.IPAddress), (int)Configuration.Current.Port);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
