using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public class ServerControl
    {
        private readonly DownloadScheduler scheduler;
        private readonly WebSocketBroadcaster broadcaster;
        private readonly ILogger<ServerControl> logger;
        private readonly IHostApplicationLifetime lifetime;
        private readonly DownloadRepository repository;

        public bool IsRestartRequested { get; private set; }

        public ServerControl(DownloadScheduler scheduler, WebSocketBroadcaster broadcaster, ILogger<ServerControl> logger, IHostApplicationLifetime lifetime, DownloadRepository repository)
        {
            this.scheduler = scheduler;
            this.broadcaster = broadcaster;
            this.logger = logger;
            this.lifetime = lifetime;
            this.repository = repository;
        }

        /// <summary>
        /// Lädt die Servereinstellungen neu indem der Dienst nach 3 Sekunden beendet wird,
        /// systemd wird den Dienst anschließend automatisch neustarten.
        /// </summary>
        public async Task RestartAsync()
        {
            using (await repository.Context.WriterLockAsync())
            {
                if (IsRestartRequested == false)
                {
                    IsRestartRequested = true;

                    logger.LogInformation("Request to restart the service received, the service will be restarted in 3 seconds.");

                    // Wird der Dienst in der Befehlszeile ausgeführt, kann ein Neustart fehlschlagen.
                    if (!SystemdHelpers.IsSystemdService())
                    {
                        logger.LogWarning("The service was not started via a service management, possibly the restart fails.");
                    }

                    // Alle Downloads anhalten.
                    await scheduler.Stop();

                    NotificationRecord clientNotification = new()
                    {
                        ErrorType = "Application.Server.Restart",
                        MessageLog = null
                    };

                    // Benachrichtigung an alle Clients senden, dass der Dienst neugestartet wird.
                    // TODO: Change to CommandFromServer.Notification?
                    broadcaster.Add(CommandFromServer.Error, clientNotification);

                    // Dienst nach 3 Sekunden beenden.
                    await Task.Delay(3000).ContinueWith((e) => lifetime.StopApplication());
                }
                else
                {
                    logger.LogInformation("A request to restart the service has already been sent.");
                }
            }
        }
    }
}