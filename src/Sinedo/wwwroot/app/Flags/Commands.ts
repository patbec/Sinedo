namespace Application.Flags {

    /**
     * Befehle die vom Client an den Server gesendet werden.
     */
    export enum ClientCommands {

        Start = 2,
        Stop = 3,
        Delete = 4,
        StartAll = 7,
        StopAll = 8,

        Upload = 9,
    }

    /**
     * Befehle die vom Server an den Client gesendet werden.
     */
    export enum ServerCommands {

        /**
         * Beim Verarbeiten eines Befehls ist ein Fehler aufgetreten.
         */
        Error = 1,

        /**
         * Ein neues Objekt wurde hinzugefügt.
         */
        Added = 2,

        /**
         * Ein Objekt wurde entfernt.
         */
        Removed = 3,

        /**
         * Der Status eines Objektes hat sich geändert.
         */
        Changed = 4,

        /**
         * Eine neue Verbindung wurde hergestellt, diese Nachricht enthält alle Daten.
         */
        Setup = 5,

        /**
         * Es ist eine Benachrichtigung verfügbar.
         */
        Notification = 6,

        /**
         * Es gibt neue Daten für das Monitoring.
         */
        Monitor = 9,

        /**
         * Es gibt neue Daten für den Datenträger.
         */
        DiskInfo = 7,


        BandwidthInfo = 8,
    }
}