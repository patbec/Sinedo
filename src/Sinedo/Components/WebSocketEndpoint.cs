using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components
{
    public class WebSocketEndpoint
    {
        private readonly Guid webSocketUid;
        private readonly WebSocket webSocket;

        private readonly SemaphoreSlim semaphore;

        #region Constant

        private const int StreamWriteTimeout = 3000;
        //private const int StreamReadTimeout = 3000;

        public const int StreamMessageSize = 2 * 1024;
        public const int StreamMessageSizeLimit = 10 * 1024 * 1024;

        #endregion

        #region Events

        public delegate void CommandReceivedEventHandler(WebSocketEndpoint webSocketEndpoint, WebSocketPackage webSocketPackage);
        public delegate void CommandExceptionEventHandler(WebSocketEndpoint webSocketEndpoint, Exception exception);

        public delegate void ConnectionEventHandler(WebSocketEndpoint webSocketEndpoint);

        public event CommandReceivedEventHandler CommandReceived;
        public event CommandExceptionEventHandler CommandException;

        public event ConnectionEventHandler ConnectionOpened;
        public event ConnectionEventHandler ConnectionClosed;

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
        public WebSocketEndpoint(WebSocket webSocket)
        {
            // Zugrundeliegende Verbindung.
            this.webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));

            // Identifikationsnummer für das Log.
            this.webSocketUid = Guid.NewGuid();

            // Threadsicherheit für Schreibvorgänge.
            this.semaphore = new SemaphoreSlim(1);
        }

        #endregion


        /// <summary>
        /// Sendet eine Nachricht an den Client.
        /// </summary>
        /// <param name="message">Die zu sendende Nachricht.</param>
        /// <param name="cancellationToken">Token um den Vorgang abzubrechen.</param>
        /// <exception cref="TimeoutException">Tritt auf, wenn ein anderer Thread das Senden der Nachricht blockiert.</exception>
        /// <returns>True, wenn die Nachricht erfolgreich gesendet wurde.</returns>
        public async Task<bool> Send(byte[] data, int timeout = StreamWriteTimeout, bool closeAfterCancel = false, CancellationToken cancellationToken = default)
        {
            bool result = await semaphore.WaitAsync(timeout, cancellationToken);

            if ( ! result)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                throw new TimeoutException();
            }

            try
            {
                await webSocket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

                if (closeAfterCancel && cancellationToken.IsCancellationRequested)
                {
                    await Close(WebSocketCloseStatus.EndpointUnavailable, "Connection closed by Server");
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                return false;
            }
            finally
            {
                semaphore.Release();
            }

            return true;
        }

        /// <summary>
        /// Schließt die Verbindung zum Client.
        /// <paramref name="socketStatus">Grund des Trennvorganges.</paramref>
        /// <paramref name="socketDescription">Erweiterte Beschreibung für den Grund des Trennvorganges.</paramref>
        /// </summary>
        public async Task Close(WebSocketCloseStatus socketStatus = WebSocketCloseStatus.NormalClosure, string socketDescription = null)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(socketStatus,
                                               socketDescription, CancellationToken.None);
                }
            }
            catch
            {
                // ToDo: Log => Connection Abnormally closed
            }
            finally
            {
                webSocket.Dispose();
            }
        }

        #region Public

        /// <summary>
        /// Beginnt eingehende Nachrichten zu lesen.
        /// </summary>
        public void Start()
        {
            Task.Factory.StartNew(
                ContextLoop, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler);
        }

        #endregion

        #region Private

        /// <summary>
        /// Verarbeitet eingehende Nachrichten die der Client in den Netzwerkstream geschrieben hat.
        /// </summary>
        private async Task ContextLoop()
        {
            try
            {
                // Event auslösen, dass die Verbindung geöffnet wurde.
                ConnectionOpened?.Invoke(this);

                // Nachrichten lesen bis die Verbindung geschlossen wird.
                while (webSocket.State == WebSocketState.Open)
                {
                    // Nachricht aus dem Stream lesen.
                    byte[] buffer = await ReadMessageBlockAsync();

                    // Prüfen ob die Verbindung geschlossen wurde.
                    if (buffer == null)
                        break;

                    // Event auslösen, dass eine Nachricht empfangen wurde.
                    RaiseCommandReceived(buffer);
                }
            }
            catch (Exception exception)
            {
                Close(WebSocketCloseStatus.PolicyViolation, exception.Message)
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                // Event auslösen, dass die Verbindung geschlossen wurde.
                ConnectionClosed?.Invoke(this);
            }
        }

        private void RaiseCommandReceived(byte[] buffer)
        {
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
        private async Task<byte[]> ReadMessageBlockAsync()
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
                messageCache.Write(buffer, 0, result.Count);

                // Empfangene Nachrichten dürfen nicht größer wie 10 MB sein.
                if (messageCache.Position > StreamMessageSizeLimit)
                    return null;
            }
            // Prüfen ob das Ende der Nachricht erreicht wurde.
            while ( ! result.EndOfMessage);

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
