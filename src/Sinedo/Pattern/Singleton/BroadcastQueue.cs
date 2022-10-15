using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public class BroadcastQueue
    {
        private BufferBlock<WebSocketPackage> Queue { get; } = new();

        public void Add(CommandFromServer command, object content)
        {
            Add(new WebSocketPackage(command, content));
        }

        public void Add(WebSocketPackage package)
        {
            Queue.Post(package ?? throw new ArgumentNullException(nameof(package)));
        }

        public Task<WebSocketPackage> GetItemAsync(CancellationToken cancellationToken)
        {
            return Queue.ReceiveAsync(cancellationToken);
        }
    }
}