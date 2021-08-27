namespace Sinedo.Models
{
    public record SettingsRecord
    {
        /// <summary>
        /// Benutzername für den Dienst.
        /// </summary>
        public string SharehosterUsername { get; init; }

        /// <summary>
        /// Kennwort für den Dienst.
        /// </summary>
        public string SharehosterPassword { get; init; }

        /// <summary>
        /// Geschwindigkeit der Internetverbindung in Mbits.
        /// </summary>
        public uint InternetConnectionInMbits { get; init; }

        /// <summary>
        /// Anzahl der gleichzeitigen Downloads.
        /// </summary>
        public uint ConcurrentDownloads { get; init; }
        
        /// <summary>
        /// Zielordner für die heruntergeladenen Dateien.
        /// </summary>
        public string DownloadDirectory { get; init; }
        
        /// <summary>
        /// Gibt an ob heruntergeladene Dateien entpackt werden sollen.
        /// </summary>
        public bool IsExtractingEnabled { get; init; }

        /// <summary>
        /// Zielordner für die entpackten Dateien.
        /// </summary>
        public string ExtractingDirectory { get; init; }
    }
}