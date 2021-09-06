namespace Sinedo.Models
{
    public record HyperlinkRecord
    {
        /// <summary>
        /// Adresse des Links.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Anzeigename in der Oberfläche.
        /// </summary>
        public string DisplayName { get; set; }
    }
}