namespace Sinedo.Models
{
    public record SetupRecord
    {
        /// <summary>
        /// Gibt Informationen über den Server zurück.
        /// </summary>
        public SystemRecord SystemInfo { get; init; }

        /// <summary>
        /// Gibt die Anzahl der insgesamt gelesenen Bytes zurück.
        /// </summary>
        public DiskSpaceRecord DiskInfo { get; init; }

        /// <summary>
        /// Gibt die Auslastung der Internetverbindung zurück.
        /// </summary>
        public BandwidthRecord BandwidthInfo { get; init; }

        /// <summary>
        /// Gibt eine Auflistung von Gruppen zurück.
        /// </summary>
        public DownloadRecord[] Downloads { get; init; }
    }
}
