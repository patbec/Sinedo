
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Models;

namespace Sinedo.Hosted
{
    public class AutoDiscovery : IHostedService
    {
        private readonly ILogger<AutoDiscovery> logger;
        private readonly byte[] magicPacketBytes = new byte[] { 0x2, 0x2, 0x2, 0x2 };
        private readonly int autoDiscoveryPort = 2222;

        private Task listener;
        private CancellationTokenSource cancellationTokenSource;

        public AutoDiscovery(ILogger<AutoDiscovery> logger) {
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            lock(this)
            {
                cancellationTokenSource = new CancellationTokenSource();
                listener = Task.Factory.StartNew(SendDiscoveryMessages, cancellationTokenSource.Token);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            lock(this)
            {
                cancellationTokenSource.Cancel();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sendet UDP-Broadcast Pakete mit der IP-Adresse und dem MaschineName des Servers.
        /// </summary>
        private void SendDiscoveryMessages()
        {
            try
            {
                logger.LogInformation("Discovery service started.");

                var server = new UdpClient(autoDiscoveryPort);

                while ( ! cancellationTokenSource.IsCancellationRequested)
                {
                    // Auf Nachricht von Clients warten.
                    var clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var clientRequestData = server.Receive(ref clientEndPoint);

                    // Prüfen ob dieser Server angesprochen wurde.
                    if(clientRequestData.SequenceEqual(magicPacketBytes))
                    {
                        logger.LogDebug("A client with ip {0} wants to receive the server data.", clientEndPoint.Address);

                        // Antwort Paket erstellen.
                        var response = CreateResponsePackage();
                        var responseData = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.ASCII.GetBytes(responseData);

                        // Antwort mit Hostname und IP-Adressen senden.
                        server.Send(responseBytes, responseData.Length, clientEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                // Infomeldung schreiben.
                logger.LogError(ex, "An error occurred while sending a server message.");
            }
            finally
            {
                // Infomeldung schreiben.
                logger.LogInformation("Discovery service stopped.");
            }
        }

        /// <summary>
        /// Versucht ein Info Paket vom Server zu erhalten.
        /// </summary>
        public DiscoveryRecord ReceiveDiscoveryMessage()
        {
            var udpClient = new UdpClient();
            var serverEndPoint = new IPEndPoint(IPAddress.Any, 0);

            udpClient.EnableBroadcast = true;
            udpClient.Client.ReceiveTimeout = 3000;
            udpClient.Client.SendTimeout = 3000;

            // Paket über den Broadcast an alle Server senden, der Server antwortet anschließend mit einem Hostname und IP-Adressen.
            udpClient.Send(magicPacketBytes, magicPacketBytes.Length, new IPEndPoint(IPAddress.Broadcast, autoDiscoveryPort));

            // Antwort vom Server empfangen.
            var serverResponseData = udpClient.Receive(ref serverEndPoint);
            var serverResponse = DiscoveryRecord.Parse(serverResponseData);

            udpClient.Close();

            return serverResponse;
        }

        private static DiscoveryRecord CreateResponsePackage()
        {
            var ipAdresses = new List<string>();
            var maschineName = Environment.MachineName;

            foreach (var ip in Dns.GetHostAddresses(maschineName))
            {
                ipAdresses.Add(ip.ToString());
            }

            return new ()
            {
                MachineName = maschineName,
                IPAdresses = ipAdresses.ToArray()
            };
        }
    }
}