using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Background;
using Sinedo.Components;
using Sinedo.Components.Logging;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            // Workround for https://github.com/dotnet/aspnetcore/issues/31365
#if DEBUG
#else
            bool isParameterHandled = await new CommandLine(args).ExecuteAsync();

            if (isParameterHandled)
            {
                return;
            }
#endif

            _ = Configuration.Current; // Throw if config is invalid.

            CreateHostBuilder().Build().Run();
        }

        public static IHostBuilder CreateHostBuilder() => Host
            .CreateDefaultBuilder()
            .ConfigureLogging(configure =>
            {
                configure.AddProvider(WebViewLoggerProvider.Default);
            })
            .ConfigureWebHostDefaults(configure =>
            {
                configure.CaptureStartupErrors(true);
                configure.UseKestrel(options =>
                {
                    // options.Listen(GetIPEndpoint());
                    options.UseSystemd();
                });
                configure.UseStartup<Startup>();
            });

        private static IPEndPoint GetIPEndpoint()
        {
            var httpAddress = Configuration.Current.IPAddress;
            var httpPort = Configuration.Current.Port;

            return new IPEndPoint(IPAddress.Parse(httpAddress), httpPort);
        }
    }
}