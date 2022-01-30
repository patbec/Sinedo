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
    public class BroadcasterReceiver
    {
        private readonly UdpClient udpRecClient;

        public BroadcasterReceiver(int port)
        {
            udpRecClient = new(port);
        }

        /// <summary>
        /// Wartet auf Broadcast-Nachrichten.
        /// </summary>
        public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            UdpReceiveResult receiveResult = await udpRecClient.ReceiveAsync(cancellationToken);

            return receiveResult;
        }
    }
}