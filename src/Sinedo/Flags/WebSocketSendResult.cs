using System;

namespace Sinedo.Flags
{
    /// <summary>
    /// Status wenn ein WebSocketPackage gesendet wurde. 
    /// </summary>
    public enum WebSocketSendResult : ushort
    {
        /// <summary>
        /// Das Paket konnte nicht gesendet werden.
        /// </summary>
        Failed = 0,

        /// <summary>
        /// Die Nachricht wurde erfolgreich gesendet.
        /// </summary>
        Success = 1,

        /// <summary>
        /// Dieser Client hat den angegebenen Channel nicht abonniert.
        /// </summary>
        NoChannel = 2,

        /// <summary>
        /// Der Vorgang wurde Abgebrochen.
        /// </summary>
        Canceled = 3,
    }
}
