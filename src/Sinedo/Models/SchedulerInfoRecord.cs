namespace Sinedo.Models
{
    public record SchedulerInfoRecord
    {
        /// <summary>
        /// Gibt die Anzahl von Downloads zurück.
        /// </summary>
        public int DownloadsCount { get; init; }

        /// <summary>
        /// Gibt die Anzahl von aktiven Downloads zurück.
        /// </summary>
        public int DownloadsRunning { get; init; }
    }
}
