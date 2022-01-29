namespace Application.Flags {

    export enum WebSocketChannel {
        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * <see cref="CommandFromServer.Error">Aufgetretene Fehler</see>
         * <see cref="CommandFromServer.Notification">Benachrichtigungen</see> // TODO: Remove?
         */
        Notification = 1,

        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * <see cref="CommandFromServer.DownloadAdded">Download hinzugefügt</see>
         * <see cref="CommandFromServer.DownloadRemoved">Download entfernt</see>
         * <see cref="CommandFromServer.DownloadChanged">Download status aktualisieren</see>
         * <see cref="CommandFromServer.Setup">Setup paket</see>
         */
        Downloads = 2,

        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * <see cref="CommandFromServer.Bandwidth">Bandbreitenstatus und Verlauf</see>
         * <see cref="CommandFromServer.Setup">Setup paket</see>
         */
        Bandwidth = 3,

        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * <see cref="CommandFromServer.Disk">Festplattenstatus und Verlauf</see>
         * <see cref="CommandFromServer.Setup">Setup paket</see>
         */
        Disk = 4,

        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * <see cref="CommandFromServer.Links">Gespeicherte Links</see>
         * <see cref="CommandFromServer.Setup">Setup paket</see>
         */
        Links = 5,

        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * TODO: Add missing items.
         * <see cref="CommandFromServer.Setup">Setup paket</see>
         */
        Settings = 6,

        /**
         * In diesem Channel werden folgende Pakete empfangen:
         * 
         * TODO: Add missing items.
         * <see cref="CommandFromServer.Setup">Setup paket</see>
         */
        Logs = 7
    }
}