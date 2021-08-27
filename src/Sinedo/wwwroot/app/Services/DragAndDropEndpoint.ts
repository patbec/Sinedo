/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="NotificationControl.ts" />

namespace Application.Services {

    export class DragAndDropEndpoint implements Interfaces.IServiceEndpoint {

        private _controller: Controller;
        private _fileSupportEnabled: boolean;

        /**
         * Initialisiert das Benachrichtigungs-Steuerelement.
         */
        public onactivate(ev: Controller): void {
            this._controller = ev;

            window.addEventListener("dragover",
                this.DragOverEventHandler.bind(this), false);
            window.addEventListener("drop",
                this.DropEventHandler.bind(this), false);
        }

        /**
         * Bereitet das Benachrichtigungs-Steuerelement für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
            this._fileSupportEnabled = true;
        }

        /**
         * Verarbeitet einen eingehenden Befehl.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {

        }

        /**
         * Bereinigt das Benachrichtigungs-Steuerelement.
         */
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {
            this._fileSupportEnabled = false;
        }


        private DragOverEventHandler(evt: DragEvent) {
            evt.stopPropagation();
            evt.preventDefault();

            if (this._fileSupportEnabled == true)
                evt.dataTransfer.dropEffect = "move";
            else
                evt.dataTransfer.dropEffect = "none";
        }

        private DropEventHandler(evt: DragEvent) {
            evt.stopPropagation();
            evt.preventDefault();

            if (this._fileSupportEnabled == false)
                return;

            try {
                /* Die hinzugefügten Dateien prüfen und an den Server senden. */
                for (let file of evt.dataTransfer.files) {
                    let reader: FileReader;
                    let controller = this._controller;

                    if (DragAndDropEndpoint.CheckFile(file)) {
                        reader = new FileReader();
                        reader.onload = function () {
                            let content: Interfaces.ICommandUpload =
                            {
                                fileName: file.name,
                                files: reader.result.toString().split("\n"),
                                autostart: true
                            };

                            // Wrapper um eine Eingabedatei an den Server zu senden.
                            let result = controller.send(Flags.ClientCommands.Upload, 0, content);

                            if ( ! result) {
                                console.error(`The file '${file.name}' could not be uploaded.`);
                                NotificationControl.current.addException("UploadFailed");
                            }
                        }

                        reader.readAsText(file);
                    } else {
                        console.error(`The file '${file.name}' exceeds the maximum allowed size.`);
                        NotificationControl.current.addException("UploadSizeExceeded");
                    }
                }

            } catch (e) {
                console.error("The files could not be uploaded to the server:" + e);
                NotificationControl.current.addException("UploadFailed");
            }
        }

        /**
         * Prüft die Dateigröße.
         */
        private static CheckFile(file: File): boolean {
            return file.size < 8655000;
        }

    }
}