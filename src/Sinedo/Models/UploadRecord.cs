namespace Sinedo.Models
{
    public record UploadRecord
    {
        /// <summary>
        /// Gibt den Dateinamen (mit Dateierweiterung) an.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Enthält den Inhalt.
        /// </summary>
        public string[] Files { get; set; }

        /// <summary>
        /// Kennwort zum entpacken.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gibt an, ob das Herunterladen automatisch gestartet werden soll.
        /// </summary>
        public bool Autostart { get; set; }
    }
}