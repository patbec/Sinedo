using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sinedo.Exceptions;
using Sinedo.Flags;

namespace Sinedo.Components
{
    public class WebSocketEndpoint
    {
        private readonly Guid webSocketUid;
        private readonly WebSocket webSocket;
        private readonly WebSocketChannelFilter webSocketChannelFilter;
        private readonly string friendlyName;
        private readonly long startTime;

        private readonly SemaphoreSlim semaphore;
        private readonly CancellationTokenSource cancellationTokenSource;

        #region Constant

        //private const int StreamWriteTimeout = 3000;
        //private const int StreamReadTimeout = 3000;

        public const int StreamMessageSize = 2 * 1024;
        public const int StreamMessageSizeLimit = 10 * 1024 * 1024;

        #endregion

        #region Events

        public delegate void CommandReceivedEventHandler(WebSocketEndpoint webSocketEndpoint, WebSocketPackage webSocketPackage);
        public delegate void CommandExceptionEventHandler(WebSocketEndpoint webSocketEndpoint, Exception exception);

        public delegate void ConnectionOpenedEventHandler(WebSocketEndpoint webSocketEndpoint);
        public delegate void ConnectionClosedEventHandler(WebSocketEndpoint webSocketEndpoint, Exception exception);

        public event CommandReceivedEventHandler CommandReceived;
        public event CommandExceptionEventHandler CommandException;

        public event ConnectionOpenedEventHandler ConnectionOpened;
        public event ConnectionClosedEventHandler ConnectionClosed;


        #endregion

        #region Properties

        /// <summary>
        /// Gibt die einmalige Identifikationsnummer dieser Verbindung zurück.
        /// </summary>
        public Guid Uid
        {
            get => webSocketUid;
        }

        /// <summary>
        /// Gibt den eingestellten Nachrichten-Filter zurück.
        /// </summary>
        public WebSocketChannelFilter Filter
        {
            get => webSocketChannelFilter;
        }

        /// <summary>
        /// Gibt den Anzeigenamen der Anwendung zurück.
        /// </summary>
        public string FriendlyName
        {
            get => friendlyName;
        }

        /// <summary>
        /// Gibt die Uhrzeit (UTC) zurück, wann die Verbindung erstellt wurde.
        /// </summary>
        public long StartTime
        {
            get => startTime;
        }

        /// <summary>
        /// Gibt einen Wert zurück, ob die Verbindung geöffnet ist.
        /// </summary>
        public bool IsOpend
        {
            get => webSocket.State == WebSocketState.Open;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Erstellt einen neuen verwalteten WebSocket-Endpunkt.
        /// </summary>
        public WebSocketEndpoint(WebSocket webSocket, WebSocketChannel[] webSocketChannels, string friendlyName)
        {
            // Zugrundeliegende Verbindung.
            this.webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));

            // Nachrichten-Filter der Verbindung.
            this.webSocketChannelFilter = new WebSocketChannelFilter(webSocketChannels ?? throw new ArgumentNullException(nameof(webSocketChannels)));

            // Anzeigenamen der Verbindung.
            this.friendlyName = friendlyName ?? throw new ArgumentNullException(nameof(friendlyName));

            // Uhrzeit (UTC) wann die Verbindung erstellt wurde.
            this.startTime = DateTime.UtcNow.Ticks;

            // Identifikationsnummer für das Log.
            this.webSocketUid = Guid.NewGuid();

            // Threadsicherheit für Schreibvorgänge.
            this.semaphore = new SemaphoreSlim(1);

            // Token um die Verbindung zu schließen.
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Public

        /// <summary>
        /// Sendet eine Nachricht an den Client.
        /// </summary>
        /// <param name="message">Die zu sendende Nachricht.</param>
        /// <param name="cancellationToken">Token um den Vorgang abzubrechen.</param>
        /// <returns>True, wenn die Nachricht erfolgreich gesendet wurde.</returns>
        public async Task<WebSocketSendResult> Send(WebSocketPackage package, CancellationToken cancellationToken = default)
        {
            if (!webSocketChannelFilter.IsCommandSupported(package.Command))
            {
                return WebSocketSendResult.NoChannel;
            }

            await semaphore.WaitAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return WebSocketSendResult.Canceled;
            }

            if (webSocket.State != WebSocketState.Open)
            {
                return WebSocketSendResult.Failed;
            }

            try
            {
                await webSocket.SendAsync(package.GetBuffer(), WebSocketMessageType.Binary, true, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return WebSocketSendResult.Canceled;
                }
            }
            catch
            {
                cancellationTokenSource.Cancel();
                return WebSocketSendResult.Failed;
            }
            finally
            {
                semaphore.Release();
            }

            return WebSocketSendResult.Success;
        }

        public void Close()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Beginnt eingehende Nachrichten zu lesen.
        /// </summary>
        public async Task Start()
        {
            await ContextLoop();
            // Task.Factory.StartNew(
            //     ContextLoop, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler);
        }

        #endregion

        #region Private

        /// <summary>
        /// Verarbeitet eingehende Nachrichten die der Client in den Netzwerkstream geschrieben hat.
        /// </summary>
        private async Task ContextLoop()
        {
            Exception lastException = null;

            try
            {
                // Event auslösen, dass die Verbindung geöffnet wurde.
                ConnectionOpened?.Invoke(this);

                // Nachrichten lesen bis die Verbindung geschlossen wird.
                while (webSocket.State == WebSocketState.Open)
                {
                    // Nachricht aus dem Stream lesen.
                    byte[] buffer = await ReadMessageAsync();

                    // Prüfen ob die Verbindung geschlossen wurde.
                    if (buffer == null)
                        break;

                    // Event auslösen, dass eine Nachricht empfangen wurde.
                    RaiseCommandReceived(buffer);
                }
            }
            catch (OperationCanceledException)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            catch (PolicyViolationException policyViolation)
            {
                lastException = policyViolation;
                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, policyViolation.Message, CancellationToken.None);
            }
            catch (Exception exception)
            {
                lastException = exception;
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, exception.Message, CancellationToken.None);
            }
            finally
            {
                webSocket.Dispose();

                // Event auslösen, dass die Verbindung geschlossen wurde.
                ConnectionClosed?.Invoke(this, lastException);
            }
        }

        private void RaiseCommandReceived(byte[] buffer)
        {
            WebSocketPackage webSocketPackage = new(buffer);

            bool isCommandAllowed = webSocketChannelFilter.IsCommandSupported(webSocketPackage.Command);

            if (!isCommandAllowed)
            {
                throw new PolicyViolationException(this, webSocketPackage);
            }

            try
            {
                // Event auslösen, dass eine Nachricht empfangen wurde.
                CommandReceived?.Invoke(this, new WebSocketPackage(buffer));
            }
            catch (Exception exception)
            {
                // Event auslösen, dass ein Fehler aufgetreten ist.
                CommandException.Invoke(this, exception);
            }
        }

        /// <summary>
        /// Liest Daten aus dem zugrundeliegenden Stream bis zu einem EOF (EndOfMessage) Signal.
        /// </summary>
        /// <returns>Empfangene Daten, Null wenn der Stream geschlossen wurde.</returns>
        private async Task<byte[]> ReadMessageAsync()
        {
            // Cache für die empfangenen Daten erstellen.
            using MemoryStream messageCache = new();

            var buffer = new byte[StreamMessageSize];
            var result = default(WebSocketReceiveResult);

            do
            {

                // Chunk aus dem Stream lesen.
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                // Chunk in den Cache schreiben.
                await messageCache.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, result.Count));

                // Empfangene Nachrichten dürfen nicht größer wie 10 MB sein.
                if (messageCache.Position > StreamMessageSizeLimit)
                    return null;
            }
            // Prüfen ob das Ende der Nachricht erreicht wurde.
            while (!result.EndOfMessage);

            // Prüfen ob die Verbindung geschlossen wurde.
            if (result.CloseStatus.HasValue)
                return null;

            // Buffer abschneiden.
            messageCache.Flush();
            messageCache.Capacity = (int)messageCache.Position;

            // Keine Kopie erstellen.
            return messageCache.GetBuffer();
        }

        #endregion
    }
}
