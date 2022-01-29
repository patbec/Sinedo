
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
        private static readonly int autoDiscoveryPort = 2222;


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

                await ListenAndSendMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Stopped
            }
            // catch (Exception ex)
            // {
            //     logger.LogCritical(ex, "An error occurred while sending a server message.");
            // }
            finally
            {
                logger.LogInformation("Discovery service stopped.");
            }
        }

        private async Task ListenAndSendMessagesAsync(CancellationToken stoppingToken)
        {
            using UdpClient udpRecClient = new(autoDiscoveryPort);
            using UdpClient udpSendClient = new();

            // // Antwort Paket mit Anzeigenamen und aktuellen Urls erstellen.
            // DiscoveryRecord discoveryInfo = GetServerDiscoveryInfo();

            // // Auf Anfragen im Netzwerk warten.
            // UdpReceiveResult receiveResult = await udpRecClient.ReceiveAsync(stoppingToken);

            // // Antwort ins Netzwerk Broadcasten.
            // await udpSendClient.SendAsync(discoveryInfo.AsMemory(), new IPEndPoint(IPAddress.Broadcast, autoDiscoveryPort), stoppingToken);

            // return;
            // // // Ein erstes Hallo-Paket senden.
            // // await SendPackageAsync(udpSendClient, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Auf Anfragen im Netzwerk warten.
                    UdpReceiveResult receiveResult = await udpRecClient.ReceiveAsync(stoppingToken);


                    // Prüfen ob dieser Dienst angesprochen wurde.
                    if (!receiveResult.Buffer.SequenceEqual(magicPacketBytes))
                    {
                        continue;
                    }

                    logger.LogDebug("A client with ip {ipAddress} wants to receive the server data.", receiveResult.RemoteEndPoint.Address);

                    // Antwort ins Netzwerk Broadcasten.
                    await udpSendClient.SendAsync(GetServerDiscoveryInfo().AsMemory(), new IPEndPoint(IPAddress.Broadcast, autoDiscoveryPort), stoppingToken);

                    // // Sendet eine Antwort mit dem Servernamen und den aktuellen Urls.
                    // await SendPackageAsync(udpSendClient, stoppingToken);

                    // 1 Sekunde warten um Spam zu verhindern.
                    //await Task.Delay(100, stoppingToken);
                }
                catch (SocketException se)
                {
                    logger.LogWarning(se, "An error occurred while trying to receive broadcast packets. It will be tried again in 5 minutes.");

                    // 5 Minuten warten.
                    await Task.Delay(300000, stoppingToken);
                }
            }
        }

        private async Task SendPackageAsync(UdpClient udpClient, CancellationToken stoppingToken)
        {
            // Antwort Paket mit Anzeigenamen und aktuellen Urls erstellen.
            DiscoveryRecord discoveryInfo = GetServerDiscoveryInfo();

            // Antwort ins Netzwerk Broadcasten.
            await udpClient.SendAsync(discoveryInfo.AsMemory(), stoppingToken);

            logger.LogDebug("A package has been sent.");
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Discovery service stopping.");

            await base.StopAsync(stoppingToken);
        }

        private string GetDisplayName()
        {
            string displayName = configuration.ApplicationName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = Environment.MachineName;
            }

            if (environment.IsDevelopment())
            {
                displayName += " (Development)";
            }

            return displayName;
        }

        private DiscoveryRecord GetServerDiscoveryInfo()
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
                    urls.Add("http://" + ipAddress);
                }
            }

            return new()
            {
                DisplayName = GetDisplayName(),
                Urls = urls.ToArray()
            };
        }

        /// <summary>
        /// Sucht die angegebene Anzahl von Sekunden, im Netzwerk nach anderen Servern.
        /// </summary>
        public static async IAsyncEnumerable<DiscoveryRecord> FindServerAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using UdpClient udpClient = GetClient();

            var bufferToSend = new ReadOnlyMemory<byte>(magicPacketBytes, 0, magicPacketBytes.Length);

            // Anfrage über den Broadcast senden, alle Server Antworten anschließend mit einem Paket das Anzeigenamen und Urls enthält.
            await udpClient.SendAsync(bufferToSend, GetEndpoint(), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Antworten von anderen Servern asynchron lesen.
                var receiveResult = await udpClient.ReceiveAsync(cancellationToken);

                yield return DiscoveryRecord.Parse(receiveResult.Buffer);
            }
        }

        private static IPEndPoint GetEndpoint()
        {
            return new IPEndPoint(IPAddress.Broadcast, autoDiscoveryPort);
        }

        private static UdpClient GetClient()
        {
            UdpClient udpClient = new()
            {
                EnableBroadcast = true
            };

            udpClient.Client.SendTimeout = 3000;

            return udpClient;
        }
    }
}