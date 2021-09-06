/// <reference path="../Flags/State.ts" />
/// <reference path="../Flags/Meta.ts" />

namespace Application.Interfaces
{
    /**
     * Schnittstelle für ein Setup-Paket.
     */
     export interface ICommandSetup {

        /**
         * Gibt den Hostname des Servers zurück.
         */
        systemInfo: ICommandSystem;

        /**
         * Gibt die Anzahl der insgesamt gelesenen Bytes zurück.
         */
        bytesTotalDownload: number;

        /**
         * Gibt eine Auflistung von Gruppen zurück.
         */
        downloads: Array<IDownloadRecord>;

        /**
         * Gibt Informationen über den verfügbaren Speicherplatz zurück.
         */
        diskInfo: ICommandDiskSpace;

        /**
         * Gibt Informationen über die Auslastung der Internetverbindung zurück.
         */
        bandwidthInfo: ICommandBandwidth;

        /**
         * Gibt Informationen über gespeicherte Links zurück.
         */
        links: ICommandHyperlink[];
    }

    /**
     * Schnittstelle für ein Gruppen-Paket.
     */
    export interface IDownloadRecord
    {
        /**
         * Einmalige Identifikationszeichenfolge.
         */
        name: string;

        /**
         * Informationen über den Status.
         */
        state: Application.Flags.State;

        /**
         * Besitzt State den Zustand 'Running' werden
         * hier genauere Informationen gespeichert.
         */
        meta: Application.Flags.Meta;

        /**
         * Inhalt des Torrents.
         */
        content: string;

        /**
         * Letzte Fehlermeldung.
         */
        lastException: string;

        /**
         * Gelesene Bytes seit der letzten Aktualisierung.
         */
        bytesPerSecond?: number;

        /**
         * Uhrzeit wann der aktuelle Vorgang abgeschlossen ist.
         */
        secondsToComplete?: number;

        /**
         * Fortschritt in Prozent.
         */
        groupPercent?: number;
    }

    export interface ICommandSystem {

        hostname: string;
        platform: string;
        architecture: string;
        pid: number;
        version: string;
    }

    export interface ICommandHyperlink {

        url: string;
        displayName: string;
    }

    /**
     * Schnittstelle für ein Error-Paket.
     */
    export interface ICommandMessage {

        /**
         * Gibt den Fehlertype zurück.
         */
        errorType: string;

        /**
         * Gibt die interne Meldung für die Console zurück.
         */
         messageLog: string;
    }

    /**
     * Schnittstelle für ein BandwidthInfo-Paket.
     */
    export interface ICommandBandwidth {
        bytesReadTotal: number;
        bytesRead: number;

        data: Array<number>;
    }

    /**
     * Schnittstelle für ein DiskInfo-Paket.
     */
     export interface ICommandDiskSpace {
        totalSize: number;
        freeBytes: number;
        data: Array<number>;
    }

    /**
     * Schnittstelle für ein Monitor-Paket.
     */
    export interface IMonitorRecord {

        /**
         * Gibt die Netzwerkauslastung in Prozent zurück.
         */
        workload: number;

        /**
         * Gibt die gelesenen Bytes zurück.
         */
        bytesRead: number;
    }

    /**
     * Schnittstelle für ein Upload-Paket.
     */
    export interface ICommandUpload {

        /**
         * Gibt den Dateinamen (mit Dateierweiterung) an.
         */
         fileName: string;

        /**
         * Enthält den Inhalt.
         */
        files: string[];

        /**
         * Gibt an, ob das Herunterladen automatisch gestartet werden soll.
         */
        autostart: boolean;
    }
}
