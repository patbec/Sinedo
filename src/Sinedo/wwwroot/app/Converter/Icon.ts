/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/State.ts" />

namespace Application.Converter {

    export class Icon {

        /**
         * Konvertiert den angegebenen Status zu einem Symbol. 
         */
        public static Convert(value: Object): string {

            let group = value as Interfaces.IDownloadRecord;

            switch(group.state) {
                case Application.Flags.State.Idle: {
                    return "cloud_queue";
                }
                case Application.Flags.State.Running: {
                    return "cloud_download";
                }
                case Application.Flags.State.Failed: {
                    return "error_outline";
                }
                case Application.Flags.State.Completed: {
                    return "cloud_done";
                }
                case Application.Flags.State.Queued: {
                    return "cloud";
                }
                case Application.Flags.State.Canceled: {
                    return "cloud_off";
                }
                case Application.Flags.State.Stopping: {
                    return "cloud_download";
                }
                case Application.Flags.State.Deleting: {
                    return "cloud_off";
                }
                default: {
                    return "help_outline";
                }
            }
        }
    }
}