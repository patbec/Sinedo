using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sinedo.Background;
using Sinedo.Components;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    /// <summary>
    /// Modell um Befehle an das Backend weiterzuleiten.
    /// </summary>
    public class WebSocketRouter
    {
        private readonly WebSocketConnections serviceConnections;
        private readonly WebSocketPing servicePing;
        private readonly DownloadScheduler serviceScheduler;
        private readonly DownloadRepository serviceRepository;
        private readonly HyperlinkManager serviceHyperlink;
        private readonly SetupBuilder serviceSetup;
        private readonly ServerControl serviceControl;
        private readonly ILogger<WebSocketRouter> serviceLogger;

        private static readonly SystemRecord systemInfo = SystemRecord.GetSystemInfo();

        /// <summary>
        /// Erstellt eine neues Modell um Befehle an das Backend weiterzuleiten.
        /// </summary>
        public WebSocketRouter(WebSocketConnections serviceConnections, WebSocketPing servicePing, DownloadScheduler serviceScheduler, DownloadRepository serviceRepository, HyperlinkManager serviceHyperlink, SetupBuilder serviceSetup, ServerControl serviceControl, ILogger<WebSocketRouter> serviceLogger)
        {
            this.serviceConnections = serviceConnections;
            this.servicePing = servicePing;
            this.serviceScheduler = serviceScheduler;
            this.serviceRepository = serviceRepository;
            this.serviceHyperlink = serviceHyperlink;
            this.serviceSetup = serviceSetup;
            this.serviceControl = serviceControl;
            this.serviceLogger = serviceLogger;
        }

        /// <summary>
        /// Eine WebSocket-Verbindung hinzufügen.
        /// </summary>
        public async Task AttachAsync(WebSocket webSocket, WebSocketChannel[] webSocketChannels, string friendlyName)
        {
            if (webSocket == null)
                throw new ArgumentNullException(nameof(webSocket));

            // Verwalteten WebSocket-Endpunkt erstellen.
            WebSocketEndpoint webSocketEndpoint = new(webSocket, webSocketChannels, friendlyName);

            webSocketEndpoint.CommandReceived +=
                WebSocketEndpoint_CommandReceived;
            webSocketEndpoint.CommandException +=
                WebSocketEndpoint_CommandException;
            webSocketEndpoint.ConnectionOpened +=
                WebSocketEndpoint_ConnectionOpened;
            webSocketEndpoint.ConnectionClosed +=
                WebSocketEndpoint_ConnectionClosed;

            // Beginne den Netzwerkstream zu lesen.
            await webSocketEndpoint.Start();
        }

        /// <summary>
        /// Tritt auf, wenn das Lesen von Daten aus dem Netzwerkstream beginnt.
        /// </summary>
        private async void WebSocketEndpoint_ConnectionOpened(WebSocketEndpoint webSocketEndpoint)
        {
            try
            {
                using (await serviceRepository.Context.ReaderLockAsync())
                {
                    // Eine Kopie der Downloads für das Setup-Paket erstellen.
                    DownloadRecord[] downloads = serviceRepository.AsEnumerable().ToArray();

                    // Setup-Paket das an den Client gesendet wird.
                    SetupRecord setup = new()
                    {
                        SystemInfo = systemInfo,
                        Downloads = downloads,
                        DiskInfo = serviceSetup.DiskInfo,
                        BandwidthInfo = null,
                        //Links = serviceSetup.Hyperlink, // ToDo: Fix Thread Safe Issue
                    };

                    WebSocketPackage webSocketPackage = new(CommandFromServer.Setup, WebSocketPackage.PARAMETER_UNSET, setup);

                    // Setup-Paket an den Client senden.
                    await webSocketEndpoint.Send(webSocketPackage);

                    // Verbindung zum Cache hinzufügen.
                    serviceConnections.Add(webSocketEndpoint);
                };
            }
            catch
            {
                // Log
            }
            finally
            {
            }
        }

        /// <summary>
        /// Tritt auf, wenn eine Verbindung geschlossen wird.
        /// </summary>
        private void WebSocketEndpoint_ConnectionClosed(WebSocketEndpoint webSocketEndpoint, Exception exception)
        {
            webSocketEndpoint.CommandReceived -=
                WebSocketEndpoint_CommandReceived;
            webSocketEndpoint.CommandException -=
                WebSocketEndpoint_CommandException;
            webSocketEndpoint.ConnectionOpened -=
                WebSocketEndpoint_ConnectionOpened;
            webSocketEndpoint.ConnectionClosed -=
                WebSocketEndpoint_ConnectionClosed;

            // Verbindung aus dem Cache entfernen.
            serviceConnections.Remove(webSocketEndpoint);

            if (exception != null)
            {
                serviceLogger.LogWarning(exception, "Client {uid} has terminated the connection with an exception.", webSocketEndpoint.Uid);
            }
        }

        /// <summary>
        /// Tritt auf, wenn ein Befehl empfangen wurde.
        /// </summary>
        private async void WebSocketEndpoint_CommandReceived(WebSocketEndpoint webSocketEndpoint, WebSocketPackage webSocketPackage)
        {
            switch (webSocketPackage.Command)
            {
                case CommandFromClient.Start:
                    {
                        var groupToStart = webSocketPackage.ReadContentAs<GroupActionRecord>();
                        await serviceScheduler.StartAsync(groupToStart.Name);
                        break;
                    }
                case CommandFromClient.Stop:
                    {
                        var groupToStop = webSocketPackage.ReadContentAs<GroupActionRecord>();
                        await serviceScheduler.StopAsync(groupToStop.Name);
                        break;
                    }
                case CommandFromClient.Delete:
                    {
                        var groupToDelete = webSocketPackage.ReadContentAs<GroupActionRecord>();
                        await serviceScheduler.DeleteAsync(groupToDelete.Name);
                        break;
                    }
                case CommandFromClient.StartAll:
                    {
                        await serviceScheduler.StartAllAsync();
                        break;
                    }
                case CommandFromClient.StopAll:
                    {
                        await serviceScheduler.StopAllAsync();
                        break;
                    }
                case CommandFromClient.Restart:
                    {
                        await serviceControl.RestartAsync();
                        break;
                    }
                case CommandFromClient.FileUpload:
                    {
                        var fileToUpload = webSocketPackage.ReadContentAs<UploadRecord>();

                        string name = Path.GetFileNameWithoutExtension(fileToUpload.FileName);
                        await serviceScheduler.CreateAsync(name, fileToUpload.Files, fileToUpload.Password, fileToUpload.Autostart);
                        break;
                    }
                case CommandFromClient.Links:
                    {
                        var links = webSocketPackage.ReadContentAs<HyperlinkRecord[]>();

                        serviceHyperlink.SetLinks(links);
                        break;
                    }
                case CommandFromClient.Pong:
                    {
                        var pingData = webSocketPackage.ReadContentAs<PingRecord>();

                        servicePing.PongReceived(webSocketEndpoint, pingData);
                        break;
                    }
                default:
                    {
                        throw new CommandSupportedException();
                    }
            }
        }

        /// <summary>
        /// Tritt auf, wenn beim Ausführen eines Befehls ein Fehler aufgetreten ist.
        /// </summary>
        private async void WebSocketEndpoint_CommandException(WebSocketEndpoint webSocketEndpoint, Exception exception)
        {
            WebSocketPackage webSocketPackage = new(CommandFromServer.Error, NotificationRecord.FromException(exception));

            await webSocketEndpoint.Send(webSocketPackage);
        }
    }
}
