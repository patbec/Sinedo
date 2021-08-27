using System;
using System.Collections.Generic;
using System.Text;

namespace Sinedo.Flags
{
    /// <summary>
    /// Befehle die vom Client an den Server gesendet werden.
    /// </summary>
    public enum CommandFromClient : byte
    {
        /// <summary>
        /// Vorgang starten oder wiederholen.
        /// </summary>
        Start = 2,

        /// <summary>
        /// Vorgang abbrechen.
        /// </summary>
        Stop = 3,

        /// <summary>
        /// Vorhandene Gruppe entfernen.
        /// </summary>
        Delete = 4,

        /// <summary>
        /// Startet alle Gruppen die den Status Hinzugefügt, Abgebrochen oder Fehlgeschlagen besitzen.
        /// </summary>
        StartAll = 7,

        /// <summary>
        /// Hält alle laufenden Gruppen an.
        /// </summary>
        StopAll = 8,

        Upload = 9,

    }
}
