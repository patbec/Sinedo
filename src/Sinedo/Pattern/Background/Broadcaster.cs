
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Singleton;

namespace Sinedo.Background
{
    public class Broadcaster : BackgroundService
    {
        private readonly Queues queues;
        private readonly WebSocketConnections connections;
        private readonly ILogger<Broadcaster> logger;

        /// <summary>
        /// Erstellt einen neuen Dienst um Pakete an verbundene Clients zu senden.
        /// </summary>
        public Broadcaster(Queues queues, WebSocketConnections connections, ILogger<Broadcaster> logger)
        {
            this.queues = queues;
            this.connections = connections;
            this.logger = logger;
        }

        /// <summary>
        /// Sendet Pakete aus der Warteschlange an verbundene Clients.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Broadcaster started.");

                while (true)
                {
                    WebSocketPackage package = await GetDataAsync(stoppingToken);

                    List<Task> tasks = new();

                    foreach (WebSocketEndpoint connection in connections.GetConnections())
                    {
                        tasks.Add(
                            connection.Send(
                                package, stoppingToken).ContinueWith(_ => logger.LogWarning("Packet could not be sent to client {friendlyName}.", connection.FriendlyName), TaskContinuationOptions.OnlyOnFaulted));
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
                // Canceled
            }
            finally
            {
                logger.LogInformation("Broadcaster stopped.");
            }
        }

        private Task<WebSocketPackage> GetDataAsync(CancellationToken stoppingToken) => queues.BroadcastQueue.ReceiveAsync(stoppingToken);

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Broadcaster stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}