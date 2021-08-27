using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sinedo.Components;
using Sinedo.Flags;

namespace Sinedo.Models
{
    public record DownloadRecord
    {
        /// <summary>
        /// Anzeigename des Torrents.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Informationen über den Status.
        /// </summary>
        public GroupState State { get; init; }

        /// <summary>
        /// Besitzt GroupState den Zustand 'Running' werden
        /// hier genauere Informationen gespeichert.
        /// </summary>
        public GroupMeta? Meta { get; init; }

        /// <summary>
        /// Optinales Kennwort zum entschlüsseln des Inhaltes.
        /// </summary>
        public string Password { get; init; }

        /// <summary>
        /// Links des Torrents.
        /// </summary>
        public string[] Files { get; init; }

        /// <summary>
        /// Letzte Fehlermeldung.
        /// </summary>
        public string LastException { get; init; }

        /// <summary>
        /// Datum / Uhrzeit wann der Download fertigstellt oder abgeschlossen wurde.
        /// </summary>
        public long? SecondsToComplete { get; init; }

        /// <summary>
        /// Fortschritt in Prozent.
        /// </summary>
        public int? GroupPercent { get; init; }

        /// <summary>
        /// Gelesene Bytes pro Sekunde.
        /// </summary>
        public long? BytesPerSecond { get; init; }
    }
}