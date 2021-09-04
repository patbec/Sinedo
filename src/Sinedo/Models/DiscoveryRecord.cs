using System.Text.Json;

namespace Sinedo.Models
{
    public record DiscoveryRecord
    {
        public string MachineName { get; init; }
        public string[] IPAdresses { get; init; }

        public static DiscoveryRecord Parse(byte[] data) {
            return JsonSerializer.Deserialize<DiscoveryRecord>(data);
        }
    }
}