/// <reference path="WebSocketCodes.ts" />
/// <reference path="../../Flags/Commands.ts" />

namespace Application.Common.ExWebSocket {

    export class WebSocketMessage {

        public readonly command: number | Flags.ClientCommands | Flags.ServerCommands;
        public readonly parameter: number;
        public readonly message: any;

        public constructor(command: number | Flags.ServerCommands | Flags.ClientCommands, parameter: number, message: any) {
            this.command = command;
            this.parameter = parameter;
            this.message = message;
        }

        /**
         * Erstellt aus den angegebenen Werten ein Paket, dass an den Server gesendet werden kann.
         **/
        public getbytes(): ArrayBuffer {

            let buffer: ArrayBuffer = null;
            let bufferView: DataView = null;;

            try {
                if (this.message != null) {
                    let contentViewer = new TextEncoder();
                    let contentBytes: Uint8Array;

                    contentBytes = contentViewer.encode(
                        JSON.stringify(this.message));

                    let base = new Uint8Array(contentBytes.length + 2);
                    base.set(contentBytes, 2);

                    buffer = base.subarray(0, contentBytes.length + 2).buffer;
                } else {
                    buffer = new ArrayBuffer(2);
                }

                bufferView = new DataView(buffer);
                bufferView.setInt8(0, this.command);
                bufferView.setInt8(1, this.parameter);

                return bufferView.buffer;

            } catch (e) {
                throw new Error("Creation of a WebSocket package failed: " + e);
            }
        }

        /**
         * Erstellt auf den angegebenen Bytes ein Event-Paket.
         * @param buffer Das vom Server empfangene WebSocket-Datenpaket.
         */
        public static parse(buffer: any): WebSocketMessage {
            let bufferView = new DataView(buffer);

            let command: number = bufferView.getInt8(0);
            let parameter: number = bufferView.getInt8(1);
            let message: any = null;

            /* Prüfen ob das Paket einen Inhalt besitzt. */
            if (bufferView.byteLength > 2) {

                /* Byte-Array aus dem Inhalt erstellen. */
                let data = new Uint8Array(bufferView.buffer,
                                          2,
                                          bufferView.buffer.byteLength - 2);

                /* Byte-Array zu einer Zeichenfolge konvertieren. */
                let content: string = new TextDecoder().decode(data);

                message = JSON.parse(content);
            }

            return new WebSocketMessage(command, parameter, message);
        }
    }
}