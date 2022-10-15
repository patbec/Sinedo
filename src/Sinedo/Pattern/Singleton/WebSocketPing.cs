using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Sinedo.Background;
using Sinedo.Components;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    /// <summary>
    /// Modell um die Verbindung zum Client offen zu halten und den Ping zu messen.
    /// </summary>
    public class WebSocketPing
    {
        private readonly WebSocketConnections serviceConnections;
        private readonly BroadcastQueue serviceBroadcaster;
        private readonly Timer timer;

        private readonly int KEEP_ALIVE_INTERVAL = 30000;

        private bool isRunning;
        private long timestampServer = 0;
        private TaskCompletionSource completionSource;
        private Dictionary<WebSocketEndpoint, long> cache;
        private ClientRecord data;

        /// <summary>
        /// Erstellt eine neues Modell um die Verbindung zum Client offen zu halten und den Ping zu messen.
        /// </summary>
        public WebSocketPing(WebSocketConnections serviceConnections, BroadcastQueue serviceBroadcaster)
        {
            this.serviceConnections = serviceConnections;
            this.serviceBroadcaster = serviceBroadcaster;

            //this.timer = new Timer(TimerCallback, null, 7000, KEEP_ALIVE_INTERVAL);

            data = new ClientRecord()
            {
                IsRunning = false,
            };
        }

        private void BroadcastStatus()
        {
            data = data with
            {
                IsRunning = isRunning,
                LastKeepAlivePackage = timestampServer
            };

            serviceBroadcaster.Add(CommandFromServer.Clients, data);
        }

        private void BroadcastStatus(IEnumerable<ClientItemRecord> clients)
        {
            data = data with
            {
                IsRunning = isRunning,
                LastKeepAlivePackage = timestampServer,
                Clients = clients.ToArray() ?? throw new ArgumentNullException(nameof(clients))
            };

            serviceBroadcaster.Add(CommandFromServer.Clients, data);
        }

        private void BroadcastPing()
        {
            timestampServer = DateTime.UtcNow.Ticks;
            PingRecord pingData = new()
            {
                Tick = timestampServer
            };
            serviceBroadcaster.Add(CommandFromServer.Ping, pingData);
        }

        public void TimerCallback(object state)
        {
            // Check if Running
            // Send Status to Clients (Running)
            // Send Ping over Broadcast
            // Wait if Completed or abort after 2 sec.
            // Calculate Progress
            // Send Status to Clients (Completed)
            lock (this)
            {
                // Funktion darf nicht gleichzeitig ausgeführt werden.
                if (isRunning)
                {
                    return;
                }
                isRunning = true;

                // Neuen Status 'Running' an alle Clients senden.
                BroadcastStatus();

                cache = new();
                completionSource = new();

                // Verbundene Clients cachen, Standardwert ist 0 (Timeout)
                foreach (WebSocketEndpoint connection in serviceConnections.GetConnections())
                {
                    cache.Add(connection, 0);
                }

                // Ping an alle Clients senden.
                BroadcastPing();
            }

            // Warten bis alle Clients eine Antwort gesendet haben, Timeout 2 Sekunden. 
            bool hasTimeout = completionSource.Task.Wait(2000);

            lock (this)
            {
                List<ClientItemRecord> newData = new();

                // Den empfangenen Ping-Wert zum Verlauf hinzufügen.
                foreach (var item in cache)
                {
                    // Client aussortieren wenn die Verbindung geschlossen wurde.
                    if (item.Key.IsOpend)
                    {
                        // Versuche den Ping-Verlauf abzurufen.
                        ClientItemRecord clientHistory = data.Clients.FirstOrDefault(o => o.ConnectionId == item.Key.Uid);

                        // Prüfen ob der Client eine Antwort gesendet hat.
                        if (item.Value == 0)
                        {
                            // Log Timeout
                        }

                        // Prüfen ob der Client einen Verlauf besitzt.
                        if (clientHistory == null)
                        {
                            //
                            // Der Client ist neu und besitzt keinen Verlauf.
                            //

                            // Neues Objekt erstellen und den Client zur Übersicht hinzufügen. 
                            newData.Add(new()
                            {
                                ConnectionId = item.Key.Uid,
                                FriendlyName = item.Key.FriendlyName,
                                Channels = item.Key.Filter.Channels,
                                StartTime = item.Key.StartTime,
                                PingData = new long[] { item.Value }
                            });
                        }
                        else
                        {
                            //
                            // Der Client ist bereits vorhanden und besitzt einen Verlauf.
                            //

                            // Ping-Verlauf Abrufen.
                            List<long> pingHistory = clientHistory.PingData.ToList();

                            if (pingHistory.Count > 20)
                            { // ToDo: Check
                                pingHistory.RemoveAt(0);
                            }
                            pingHistory.Add(item.Value);

                            // Objekt mit neuem Ping-Verlauf erstellen und zur Übersicht hinzufügen. 
                            newData.Add(clientHistory with
                            {
                                PingData = pingHistory.ToArray()
                            });
                        }
                    }
                }

                // Aufräumen
                cache = null;
                completionSource = null;

                // Neuen Status 'Completed' an alle Clients senden.
                isRunning = false;
                BroadcastStatus(newData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void TriggerPing()
        {
            lock (this)
            {
                if (isRunning)
                {
                    // Log: Ping is already running.
                    return;
                }

                timer.Change(0, KEEP_ALIVE_INTERVAL);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void PongReceived(WebSocketEndpoint endpoint, PingRecord pingData)
        {
            long timestampClient = DateTime.UtcNow.Ticks;

            lock (this)
            {
                if (!isRunning || cache == null)
                {
                    return;
                }

                if (cache.ContainsKey(endpoint))
                {
                    if (timestampClient <= timestampServer && pingData.Tick == timestampServer && cache[endpoint] == 0)
                    {
                        cache[endpoint] = timestampClient - timestampServer;
                    }
                }

                bool isCompleted = cache.Values.All(ticks => ticks != 0);

                if (isCompleted)
                {
                    completionSource.SetResult();
                }
            }
        }
    }
}
