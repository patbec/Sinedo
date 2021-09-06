using Sinedo.Models;

namespace Sinedo.Components
{
    /// <summary>
    /// Einstellungsdatei mit geschützten Anmeldedaten.
    /// </summary>
    public class SettingsFile
    {
        /// <summary>
        /// Das mit SHA512 gehashte Anmeldepasswort.
        /// </summary>
        public byte[] PasswordHash { get; set; }

        /// <summary>
        /// Benutzername für den Dienst.
        /// </summary>
        public string SharehosterUsername { get; set; }

        /// <summary>
        /// Kennwort für den Dienst.
        /// </summary>
        public string SharehosterPassword { get; set; }

        /// <summary>
        /// Geschwindigkeit der Internetverbindung in Mbits.
        /// </summary>
        public uint InternetConnectionInMbits { get; set; }

        /// <summary>
        /// Anzahl der gleichzeitigen Downloads.
        /// </summary>
        public uint ConcurrentDownloads { get; set; }

        /// <summary>
        /// Zielordner für die heruntergeladenen Dateien.
        /// </summary>
        public string DownloadDirectory { get; set; }

        /// <summary>
        /// Gibt an ob heruntergeladene Dateien entpackt werden sollen.
        /// </summary>
        public bool IsExtractingEnabled { get; set; }

        /// <summary>
        /// Zielordner für die entpackten Dateien.
        /// </summary>
        public string ExtractingDirectory { get; set; }
    }
}