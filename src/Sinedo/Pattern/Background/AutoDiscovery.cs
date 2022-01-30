
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.Disposables;
using Sinedo.Components;
using Sinedo.Middleware;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Background
{
    public class AutoDiscovery : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;
        private readonly ILogger<AutoDiscovery> logger;
        private static readonly byte[] magicPacketBytes = new byte[] { 0x2, 0x2, 0x2, 0x2 };
        private static readonly int autoDiscoveryControlPort = 42700;
        private static readonly int autoDiscoveryMessagePort = 42800;

        public AutoDiscovery(IConfiguration configuration, IHostEnvironment environment, ILogger<AutoDiscovery> logger)
        {
            this.configuration = configuration;
            this.environment = environment;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Discovery service started.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ListenAndSendMessagesAsync(stoppingToken);
                    }
                    catch (SocketException se)
                    {
                        logger.LogWarning(se, "An error occurred while trying to send or receive broadcast packets. It will try again in 30 seconds, the service will not be available during this time.");

                        // 30 Sekunden warten.
                        await Task.Delay(30000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Canceled
            }
            finally
            {
                logger.LogInformation("Discovery service stopped.");
            }
        }

        private async Task ListenAndSendMessagesAsync(CancellationToken stoppingToken)
        {
            var receiver = new BroadcasterReceiver(autoDiscoveryControlPort);
            var sender = new BroadcasterSender(autoDiscoveryMessagePort);

            // Startpaket senden.
            await sender.SendAsync(GetDiscoveryPackage(), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Auf Anfragen im Netzwerk warten.
                UdpReceiveResult receiveResult = await receiver.ReceiveAsync(stoppingToken);

                // Prüfen ob dieser Dienst angesprochen wurde.
                if (!receiveResult.Buffer.SequenceEqual(magicPacketBytes))
                {
                    continue;
                }

                logger.LogDebug("A client with ip {ipAddress} wants to receive the server data.", receiveResult.RemoteEndPoint.Address);

                // Sendet eine Antwort.
                await sender.SendAsync(GetDiscoveryPackage(), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Discovery service stopping.");

            await base.StopAsync(stoppingToken);
        }

        private byte[] GetDiscoveryPackage()
        {
            // Die erste Url wird beim automatischen Verbinden bevorzugt.  
            var urls = new List<string>();

            if (!string.IsNullOrWhiteSpace(configuration.ExternalUrl))
            {
                // z.B. https://my-sinedo-server.com
                // Nutze Traefik als Proxy für einen HTTPS-Zugriff.
                urls.Add(configuration.ExternalUrl);
            }
            else
            {
                // http:// + NetBIOS Name . im lokalen Netz.
                urls.Add($"http://{ Environment.MachineName }.local");

                // IPv4 und IPv6 Adressen hinzufügen.
                foreach (var ipAddress in Dns.GetHostAddresses(Environment.MachineName))
                {
                    urls.Add(Uri.UriSchemeHttp + Uri.SchemeDelimiter + ipAddress);
                }
            }

            string displayName = configuration.ApplicationName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = Environment.MachineName;
            }

            if (environment.IsDevelopment())
            {
                displayName += " (Development)";
            }

            DiscoveryRecord discoveryRecord = new()
            {
                DisplayName = displayName,
                Urls = urls.ToArray()
            };

            string content = JsonSerializer.Serialize(discoveryRecord);
            return Encoding.ASCII.GetBytes(content);
        }

        /// <summary>
        /// Sucht die angegebene Anzahl von Sekunden, im Netzwerk nach anderen Servern.
        /// </summary>
        public static async IAsyncEnumerable<DiscoveryRecord> FindServerAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sender = new BroadcasterSender(autoDiscoveryControlPort);
            var receiver = new BroadcasterReceiver(autoDiscoveryMessagePort);

            // Hallo-Paket senden.
            await sender.SendAsync(magicPacketBytes, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Antworten von anderen Servern asynchron lesen.
                UdpReceiveResult receiveResult = await receiver.ReceiveAsync(cancellationToken);

                // Gibt Null zurück, wenn das Paket nicht deserialisiert werden kann.
                yield return DiscoveryRecord.Parse(receiveResult.Buffer);
            }
        }
    }
}