
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Singleton;

namespace Sinedo.Background
{
    public class Broadcaster : BackgroundService
    {
        private readonly BroadcastQueue queue;
        private readonly WebSocketConnections connections;
        private readonly ILogger<Broadcaster> logger;

        /// <summary>
        /// Erstellt einen neuen Dienst um Pakete an verbundene Clients zu senden.
        /// </summary>
        public Broadcaster(BroadcastQueue queue, WebSocketConnections connections, ILogger<Broadcaster> logger)
        {
            this.queue = queue;
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
                    WebSocketPackage package = await queue.GetItemAsync(stoppingToken);

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

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Broadcaster stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}