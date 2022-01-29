using System;
using System.Collections.Generic;
using System.Text;

namespace Sinedo.Flags
{
    /// <summary>
    /// Befehle die vom Server an den Client gesendet werden.
    /// </summary>
    public enum CommandFromServer : byte
    {
        /// <summary>
        /// Beim Verarbeiten eines Befehls ist ein Fehler aufgetreten.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Ein neues Objekt wurde hinzugefügt.
        /// </summary>
        DownloadAdded = 2,

        /// <summary>
        /// Ein Objekt wurde entfernt.
        /// </summary>
        DownloadRemoved = 3,

        /// <summary>
        /// Der Status eines Objektes hat sich geändert.
        /// </summary>
        DownloadChanged = 4,

        /// <summary>
        /// Eine neue Verbindung wurde hergestellt, diese Nachricht enthält alle Daten.
        /// </summary>
        Setup = 5,

        /// <summary>
        /// Benachrichtigung anzeigen.
        /// </summary>
        [Obsolete("Check if needed")]
        Notification = 6,

        Disk = 7,

        Bandwidth = 8,

        Links = 9,

        Ping = 10,

        Clients = 11,
    }
}
