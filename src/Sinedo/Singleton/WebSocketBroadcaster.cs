using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly BlockingQueueSet<Tuple<CommandFromServer, int, object>> messages = new ();

        /// <summary>
        /// Erstellt ein neues Modell um Pakete an verbundene Clients zu verteilen.
        /// </summary>
        public WebSocketBroadcaster(WebSocketConnections connections)
        {
            _connections = connections;

            Task.Factory.StartNew(
                ContextLoop, TaskCreationOptions.LongRunning | TaskCreationOptions.HideScheduler);
        }

        /// <summary>
        /// Fügt am Ende der Warteschlange eine Nachricht hinzu.
        /// </summary>
        public void Add(CommandFromServer command, int parameter, object obj)
        {
            messages.Add(new (command, parameter, obj));
        }

        #region Private

        /// <summary>
        /// Sendet Pakete aus der Warteschlange an verbundene Clients.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ContextLoop()
        {
            Thread.CurrentThread.Name = nameof(WebSocketBroadcaster);

            while (true)
            {
                var messageTuple = messages.Take();
 
                if (messageTuple == null)
                    return;

                // Objekt serialisieren.
                byte[] rawPackage = WebSocketPackage.CreatePackage(
                    messageTuple.Item1,
                    messageTuple.Item2,
                    messageTuple.Item3
                    );

                WebSocketEndpoint[] clients = _connections.GetConnections();

                Task<bool>[] results = SendToAllClients(clients, rawPackage);

                Task.WaitAll(results);
            }
        }

        #endregion

        private static Task<bool>[] SendToAllClients(WebSocketEndpoint[] clients, byte[] data)
        {
            CancellationTokenSource cancellationTokenSource = new(2000);

            Task<bool>[] waitHandles = new Task<bool>[clients.Length];

            for (int i = 0; i < clients.Length; i++)
            {
                waitHandles[i] = clients[i].Send(data, 1200, closeAfterCancel: true, cancellationTokenSource.Token);
            }

            return waitHandles;
        }
    }
}
