namespace Sinedo.Models
{
    public record BandwidthRecord
    {
        public long BytesReadTotal { get; set; }
        public long BytesRead { get; set; }
        public ushort[] Data { get; set; }
    }
}