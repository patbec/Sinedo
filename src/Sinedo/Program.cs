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
            bool isParameterHandled = await new CommandLine(args).ExecuteAsync();

            if (isParameterHandled)
            {
                return;
            }

            // Throw if config is invalid.
            Configuration.LoadFile();

            await CreateHostBuilder().Build().RunAsync();
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