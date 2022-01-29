using System;
using System.Text.Json;
using Sinedo.Flags;

namespace Sinedo.Models
{
    public record ClientRecord
    {
        public bool IsRunning { get; init; }
        public long LastKeepAlivePackage { get; init; }
        public ClientItemRecord[] Clients { get; init; }
    }

    public record ClientItemRecord
    {
        public Guid ConnectionId { get; init; }
        public string FriendlyName { get; init; }
        public string IPAddress { get; init; }
        public WebSocketChannel[] Channels { get; init; }
        public long StartTime { get; init; }
        public long[] PingData { get; init; }
    }
}