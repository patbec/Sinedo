namespace Application.Flags {

    export enum State {
        /**
         * Der Vorgang wurde hinzugefügt aber nicht gestartet.
         */
        Idle = 1,
        /**
         * Der Vorgang befindet sich in der Warteschlange.
         */
        Queued = 2,
        /**
         * Der Vorgang wurde durch den Benutzer abgebrochen.
         */
        Canceled = 3,
        /**
         * Der Vorgang wurde durch einen Fehler abgebrochen.
         */
        Failed = 4,
        /**
         * Der Vorgang wird vom Aufgabenplaner ausgeführt.
         */
        Running = 5,
        /**
         * Der Vorgang wurde erfolgreich abgeschlossen.
         */
        Completed = 6,
        /**
         * Der Vorgang wird entfernt.
         */
        Deleting = 8,
        /**
         * Der Vorgang wird angehalten.
         */
         Stopping = 9,
        /**
         * Die Eingabedatei wird nicht unterstützt.
         */
        Unsupported = 10,
    }
}