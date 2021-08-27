namespace Sinedo.Models
{
    public record UploadRecord
    {
        /// <summary>
        /// Gibt den Dateinamen (mit Dateierweiterung) an.
        /// </summary>
        /// <value></value>
        public string FileName { get; set; }

        /// <summary>
        /// Enthält den Inhalt.
        /// </summary>
        /// <value></value>
        public string[] Files { get; set; }

        /// <summary>
        /// Gibt an, ob das Herunterladen automatisch gestartet werden soll.
        /// </summary>
        /// <value></value>
        public bool Autostart { get; set; }
    }
}