using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Linq;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Exceptions;
using Sinedo.Background;
using Sinedo.Models;
using System.IO;

namespace Sinedo.Singleton
{
    /// <summary>
    /// Modell um Befehle an das Backend weiterzuleiten.
    /// </summary>
    public class WebSocketRouter
    {
        private readonly WebSocketConnections serviceConnections;
        private readonly DownloadScheduler serviceScheduler;
        private readonly DownloadRepository serviceRepository;
        private readonly HyperlinkManager serviceHyperlink;
        private readonly DiskSpaceHelper serviceDiskSpaceHelper;

        private static readonly SystemRecord systemInfo = SystemRecord.GetSystemInfo();

        /// <summary>
        /// Erstellt eine neues Modell um Befehle an das Backend weiterzuleiten.
        /// </summary>
        public WebSocketRouter(WebSocketConnections serviceConnections,
                               DownloadScheduler    serviceScheduler,
                               DownloadRepository   serviceRepository,
                               HyperlinkManager     serviceHyperlink,
                               DiskSpaceHelper      serviceDiskSpaceHelper) {

            this.serviceConnections     = serviceConnections;
            this.serviceScheduler       = serviceScheduler;
            this.serviceRepository      = serviceRepository;
            this.serviceHyperlink       = serviceHyperlink;
            this.serviceDiskSpaceHelper = serviceDiskSpaceHelper;
        }

        /// <summary>
        /// Eine WebSocket-Verbindung hinzufügen.
        /// </summary>
        public void Attach(WebSocket webSocket, TaskCompletionSource<object> completionSource)
        {
            if (webSocket == null)
                throw new ArgumentNullException(nameof(webSocket));

            // Verwalteten WebSocket-Endpunkt erstellen.
            WebSocketEndpoint webSocketEndpoint = new(webSocket);

            webSocketEndpoint.CommandReceived +=
                WebSocketEndpoint_CommandReceived;
            webSocketEndpoint.CommandException +=
                WebSocketEndpoint_CommandException;
            webSocketEndpoint.ConnectionOpened +=
                WebSocketEndpoint_ConnectionOpened;
            webSocketEndpoint.ConnectionClosed +=
                WebSocketEndpoint_ConnectionClosed;
            webSocketEndpoint.ConnectionClosed +=
                (ws) => completionSource.SetResult(null);

            // Beginne den Netzwerkstream zu lesen.
            webSocketEndpoint.Start();
        }

        /// <summary>
        /// Tritt auf, wenn das Lesen von Daten aus dem Netzwerkstream beginnt.
        /// </summary>
        private void WebSocketEndpoint_ConnectionOpened(WebSocketEndpoint webSocketEndpoint)
        {
            try
            {
                serviceRepository.EnterReadLock(() =>
                {
                    // Eine Kopie der Downloads für das Setup-Paket erstellen.
                    DownloadRecord[] downloads = serviceRepository.AsEnumerable()
                                                                  .ToArray();

                    // Setup-Paket das an den Client gesendet wird.
                    SetupRecord setup = new() {
                            SystemInfo = systemInfo,
                            Downloads = downloads,
                            DiskInfo = serviceDiskSpaceHelper.DiskInfo,
                            BandwidthInfo = serviceScheduler.BandwidthInfo,
                            Links = serviceHyperlink.GetLinks()
                    };

                    // Verbindung zum Cache hinzufügen.
                    serviceConnections.Add(webSocketEndpoint);

                    byte[] content =
                        WebSocketPackage.CreatePackage(CommandFromServer.Setup, WebSocketPackage.PARAMETER_UNSET, setup);

                    // Setup-Paket an den Client senden.
                    webSocketEndpoint.Send(content)
                        .GetAwaiter()
                        .GetResult();
                });
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
        private void WebSocketEndpoint_ConnectionClosed(WebSocketEndpoint webSocketEndpoint)
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
        }

        /// <summary>
        /// Tritt auf, wenn ein Befehl empfangen wurde.
        /// </summary>
        private void WebSocketEndpoint_CommandReceived(WebSocketEndpoint webSocketEndpoint, WebSocketPackage webSocketPackage)
        {
            switch (webSocketPackage.Command)
            {
                case CommandFromClient.Start:
                    {
                        var groupToStart = webSocketPackage.ReadContentAs<GroupActionRecord>();
                        serviceScheduler.Start(groupToStart.Name);
                        break;
                    }
                case CommandFromClient.Stop:
                    {
                        var groupToStop = webSocketPackage.ReadContentAs<GroupActionRecord>();
                        serviceScheduler.Stop(groupToStop.Name);
                        break;
                    }
                case CommandFromClient.Delete:
                    {
                        var groupToDelete = webSocketPackage.ReadContentAs<GroupActionRecord>();
                        serviceScheduler.Delete(groupToDelete.Name);
                        break;
                    }
                case CommandFromClient.StartAll:
                    {
                        serviceScheduler.StartAll();
                        break;
                    }
                case CommandFromClient.StopAll:
                    {
                        serviceScheduler.StopAll();
                        break;
                    }
                case CommandFromClient.Upload:
                {
                    var fileToUpload = webSocketPackage.ReadContentAs<UploadRecord>();

                    string name = Path.GetFileNameWithoutExtension(fileToUpload.FileName);
                    serviceScheduler.Create(name, fileToUpload.Files, fileToUpload.Autostart);
                    break;
                }
                 case CommandFromClient.Links:
                {
                    var links = webSocketPackage.ReadContentAs<HyperlinkRecord[]>();

                    serviceHyperlink.SetLinks(links);
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
        private void WebSocketEndpoint_CommandException(WebSocketEndpoint webSocketEndpoint, Exception exception)
        {
            NotificationRecord clientNotification = new()
            {
                ErrorType = exception.GetType().ToString(),
                MessageLog = exception.StackTrace
            };

            byte[] data = WebSocketPackage.CreatePackage(CommandFromServer.Error, WebSocketPackage.PARAMETER_UNSET, clientNotification);

            // An den Client senden.
            webSocketEndpoint.Send(data)
                             .GetAwaiter()
                             .GetResult();
        }
    }
}
