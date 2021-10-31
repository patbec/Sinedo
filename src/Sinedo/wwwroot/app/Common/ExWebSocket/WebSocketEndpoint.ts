/// <reference path="WebSocketCloseEvent.ts" />
/// <reference path="WebSocketCodes.ts" />
/// <reference path="WebSocketMessage.ts" />

namespace Application.Common.ExWebSocket {

    export class WebSocketEndpoint {

        private _connectionSuccessful: boolean;
        private _connectionSocket: WebSocket;

        public onopened: (ev: WebSocketMessage) => any;
        public onmessage: (ev: WebSocketMessage) => any;
        public onclosed: (ev: WebSocketCloseEvent) => any;

        public get isopen(): boolean {
            return this._connectionSocket.readyState == WebSocket.OPEN;
        }

        constructor(url: string) {
            this._connectionSuccessful = false;
            this._connectionSocket = new WebSocket(url);
            this._connectionSocket.binaryType = 'arraybuffer';
            this._connectionSocket.addEventListener('message', this.socketmessage.bind(this));
            this._connectionSocket.addEventListener('close', this.socketclose.bind(this));
        }

        /**
         * Schließt die Verbindung mit dem angegebenen Code.
         */
        public close(code?: number | WebSocketCodes, reason?: string): void {
            if (this.isopen) {
                this._connectionSocket.close(code, reason);
            }
        }

        /**
         * Sendet einen Befehl an den Server.
         */
        public send(message: WebSocketMessage): boolean {
            if (this.isopen) {
                this._connectionSocket.send(message.getbytes());

                return true;
            }

            return false;
        }

        //public getConnectionPingTime(): number {
        //    this._connectionSocket.
        //}

        /**
         * Tritt auf, wenn eine Nachricht vom Server empfangen wurde.
         */
        private socketmessage(ev: MessageEvent): void {

            try {
                let message = WebSocketMessage.parse(ev.data);

                if (this._connectionSuccessful == false) {
                    this._connectionSuccessful = true;

                    this.onopened(message);
                }
                else {
                    this.onmessage(message);
                }
            }
            catch (e) {
                console.error(e);
                this._connectionSocket.close(WebSocketCodes.POLICY_VIOLATION_BY_CLIENT, e); // FixMe: Return always 1009
            }
        }

        /**
         * Tritt auf, wenn die Verbindung zum Server geschlossen wurde.
         */
        private socketclose(ev: CloseEvent): void {

            try {
                this.onclosed(
                    WebSocketCloseEvent.from(ev, this._connectionSuccessful));
            }
            catch (e) {
                console.error(e, "WebSocketEndpoint");
            }
            finally {
                if (this._connectionSocket != null) {
                    this._connectionSocket.removeEventListener('message', this.socketmessage.bind(this));
                    this._connectionSocket.removeEventListener('close', this.socketclose.bind(this));
                    this._connectionSocket = null;
                }
            }
        }

        /**
         * Erstellt eine Anwendungsspezifische URL.
         */
        public static getaddress(hostname: string, port: string, protocol: string) {
            let suffix: string

            if (protocol == "https:") {
                suffix = "wss:";
            }
            else {
                suffix = "ws:";
            }

            return `${suffix}//${hostname}:${port}/api/server-connection.ws?version=1`
        }
    }
}