namespace Sinedo.Models
{
    public record DiskSpaceRecord
    {
        public long TotalSize { get; set; }
        public long FreeBytes { get; set; }
        public ushort[] Data { get; set; }
    }
}