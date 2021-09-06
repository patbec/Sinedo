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
        Added = 2,

        /// <summary>
        /// Ein Objekt wurde entfernt.
        /// </summary>
        Removed = 3,

        /// <summary>
        /// Der Status eines Objektes hat sich geändert.
        /// </summary>
        Changed = 4,

        /// <summary>
        /// Eine neue Verbindung wurde hergestellt, diese Nachricht enthält alle Daten.
        /// </summary>
        Setup = 5,

        /// <summary>
        /// Benachrichtigung anzeigen.
        /// </summary>
        Notification = 6,

        DiskInfo = 7,

        BandwidthInfo = 8,

        LinksChanged = 9,
    }
}
