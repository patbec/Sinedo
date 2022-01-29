using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Flags;

namespace Sinedo.Singleton
{
    /// <summary>
    /// Modell um Pakete an verbundene Clients zu verteilen. 
    /// </summary>
    public class WebSocketBroadcaster
    {
        private readonly WebSocketConnections _connections;
        private readonly ILogger<WebSocketBroadcaster> _logger;

        private readonly BufferBlock<Tuple<CommandFromServer, object>> _queue = new();

        /// <summary>
        /// Erstellt ein neues Modell um Pakete an verbundene Clients zu verteilen.
        /// </summary>
        public WebSocketBroadcaster(WebSocketConnections connections, ILogger<WebSocketBroadcaster> logger)
        {
            _connections = connections;
            _logger = logger;

            var _ = Task.Run(ContextLoop);
            // Task.Factory.StartNew(
            //     ContextLoop, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler);
        }

        /// <summary>
        /// Fügt am Ende der Warteschlange eine Nachricht hinzu.
        /// </summary>
        public void Add(CommandFromServer command, object obj)
        {
            _queue.Post(new(command, obj));
        }

        #region Private

        /// <summary>
        /// Sendet Pakete aus der Warteschlange an verbundene Clients.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private async Task ContextLoop()
        {

            try
            {
                _logger.LogInformation("Broadcaster running.");

                while (await _queue.OutputAvailableAsync())
                {
                    var messageTuple = await _queue.ReceiveAsync();

                    WebSocketPackage webSocketPackage = new(messageTuple.Item1,
                                                            WebSocketPackage.PARAMETER_UNSET,
                                                            messageTuple.Item2);


                    CancellationTokenSource cancellationTokenSource = new(2000);

                    foreach (WebSocketEndpoint connection in _connections.GetConnections())
                    {
                        await connection.Send(webSocketPackage, cancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Broadcaster failed.");
            }
            finally
            {
                _logger.LogInformation("Broadcaster stopped.");
            }
        }

        #endregion
    }
}
