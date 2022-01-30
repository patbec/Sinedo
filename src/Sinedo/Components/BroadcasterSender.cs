using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components
{
    public class BroadcasterSender
    {
        private readonly int autoDiscoveryPort;

        public BroadcasterSender(int port)
        {
            autoDiscoveryPort = port;
        }

        /// <summary>
        /// Broadcastet die angegebenen Bytes über alle Netzwerkschnittstellen.
        /// </summary>
        /// <param name="data">Daten die gesendet werden sollen.</param>
        public async Task SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            using UdpClient udpSendClient = new();

            var ipEndpoint = new IPEndPoint(IPAddress.Broadcast, autoDiscoveryPort);

            await udpSendClient.SendAsync(data, ipEndpoint, cancellationToken);
        }
    }
}