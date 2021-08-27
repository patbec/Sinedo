using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Sinedo.Components;

namespace Sinedo.Singleton
{
    /// <summary>
    /// Modell um Verbindungen zu verwalten. 
    /// </summary>
    public class WebSocketConnections
    {
        private readonly List<WebSocketEndpoint> connections = new();

        /// <summary>
        /// Erstellt ein neues Modell um Verbindungen zu verwalten. 
        /// </summary>
        public WebSocketConnections()
        { }

        /// <summary>
        /// Gibt eine Auflistung von verwalteten Verbindungen zurück.
        /// </summary>
        /// <returns>Verbindungen</returns>
        public WebSocketEndpoint[] GetConnections()
        {
            WebSocketEndpoint[] result = null;

            lock (connections)
            {
                result = new WebSocketEndpoint[connections.Count];
                connections.CopyTo(result);
            }

            return result;
        }

        /// <summary>
        /// Eine Verbindung zur Verbindungsverwaltung hinzufügen.
        /// </summary>
        /// <param name="webSocket">Verbindung</param>
        public void Add(WebSocketEndpoint webSocket)
        {
            if (webSocket == null)
                throw new ArgumentNullException(nameof(webSocket));


            lock(connections)
            {
                if ( ! connections.Contains(webSocket))
                    connections.Add(webSocket);
            }
        }

        /// <summary>
        /// Entfernt die angegebene Verbindung aus der Verbindungsverwaltung.
        /// </summary>
        /// <param name="webSocket">Verbindung</param>
        public void Remove(WebSocketEndpoint webSocket)
        {
            if (webSocket == null)
                throw new ArgumentNullException(nameof(webSocket));


            lock (connections)
            {
                connections.Remove(webSocket);
            }
        }

        /// <summary>
        /// Entfernt die angegebene Verbindung aus der Verbindungsverwaltung.
        /// </summary>
        /// <param name="guid">Eindeutige Verbindungs-Id</param>
        public void Remove(Guid guid)
        {
            lock (connections)
            {
                for (int i = 0; i < connections.Count - 1; i++)
                {
                    if (connections[i].Uid == guid)
                    {
                        connections.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}
