using System;

namespace Sinedo.Flags
{
    /// <summary>
    /// Auflistung von erlaubten Channel.
    /// </summary>
    public enum WebSocketChannel : ushort
    {
        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// <see cref="CommandFromServer.Error">Aufgetretene Fehler</see>
        /// <see cref="CommandFromServer.Notification">Benachrichtigungen</see> // TODO: Remove?
        /// </summary>
        Notification = 1,

        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// <see cref="CommandFromServer.DownloadAdded">Download hinzugefügt</see>
        /// <see cref="CommandFromServer.DownloadRemoved">Download entfernt</see>
        /// <see cref="CommandFromServer.DownloadChanged">Download status aktualisieren</see>
        /// <see cref="CommandFromServer.Setup">Setup paket</see>
        /// </summary>
        Downloads = 2,

        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// <see cref="CommandFromServer.Bandwidth">Bandbreitenstatus und Verlauf</see>
        /// <see cref="CommandFromServer.Setup">Setup paket</see>
        /// </summary>
        Bandwidth = 3,

        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// <see cref="CommandFromServer.Disk">Festplattenstatus und Verlauf</see>
        /// <see cref="CommandFromServer.Setup">Setup paket</see>
        /// </summary>
        Disk = 4,

        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// <see cref="CommandFromServer.Links">Gespeicherte Links</see>
        /// <see cref="CommandFromServer.Setup">Setup paket</see>
        /// </summary>
        Links = 5,

        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// TODO: Add missing items.
        /// <see cref="CommandFromServer.Setup">Setup paket</see>
        /// </summary>
        Settings = 6,

        /// <summary>
        /// In diesem Channel werden folgende Pakete empfangen:
        /// 
        /// TODO: Add missing items.
        /// <see cref="CommandFromServer.Setup">Setup paket</see>
        /// </summary>
        Logs = 7,
    }
}
