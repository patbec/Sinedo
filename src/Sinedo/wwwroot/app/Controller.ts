/// <reference path="Common/ExWebSocket/WebSocketEndpoint.ts" />
/// <reference path="Common/ExWebSocket/WebSocketMessage.ts" />
/// <reference path="Common/ExWebSocket/WebSocketCloseEvent.ts" />
/// <reference path="Common/ExWebSocket/WebSocketTranslation.ts" />
/// <reference path="Interfaces/IServiceEndpoint.ts" />
/// <reference path="Flags/Commands.ts" />

namespace Application {

    /**
     * Controller um mit dem Server zu kommunizieren.
     */
    export class Controller {

        private readonly _serviceAddress: string;
        private readonly _serviceTimeout: number;
        private readonly _serviceEndpoints: Interfaces.IServiceEndpoint[];

        private _lastStateWasClosed = true;
        private _element: HTMLBodyElement;
        private _socket: Common.ExWebSocket.WebSocketEndpoint;

        /**
         * Gibt einen Wert zurück, wie lange gewartet werden soll
         * bis ein erneuter Verbindungsversuch gestartet wird.
         */
        public get timeout(): number {
            return this._serviceTimeout;
        }

        /**
         * Erstellt einen neuen Controller mit den angegebenen Diensten.
         * @param endpoints Endpunkte um Nachrichten zu verarbeiten.
         * @param timeout Wartezeit in ms bis ein erneuter Verbindungsversuch gestartet wird.
         */
        public constructor(endpoints: Interfaces.IServiceEndpoint[], timeout: number = 5000) {
            this._element = Application.Common.Control.get<HTMLBodyElement>("body");
            this._serviceAddress = Common.ExWebSocket.WebSocketEndpoint.getaddress(window.location.hostname,
                                                                                   window.location.port,
                                                                                   window.location.protocol);

            this._serviceEndpoints = endpoints;
            this._serviceTimeout = timeout;
            
            for (let service of this._serviceEndpoints) {
                // Auch nach Fehlern beim Aktivieren vom Diensten versuchen zu verbinden.
                // Falls Fehler von einem unwichtigen Dienst kommen kann die Anwendung trotzdem verwendet werden. 
                try {
                    service.onactivate(this);
                } catch(e) {
                    window.alert("Error on service.onactivate: " + e);
                }
            }

            this.connect();
        }

        /**
         * Sendet einen Befehl an den Server.
         * Es wird True zurückgegeben, wenn der Befehl erfolgreich gesendet wurde.
         * @param command
         * @param parameter
         * @param content
         */
        public send(command: Flags.ClientCommands, parameter: number, content: any | JSON): boolean {
            let message = new Common.ExWebSocket.WebSocketMessage(command,
                                                                  parameter,
                                                                  content);

            return this._socket.send(message);
        }

        /**
         * Versucht eine neue Verbindung herzustellen.
         */
        private connect(): void
        {
            this._socket = new Common.ExWebSocket.WebSocketEndpoint(this._serviceAddress);
            this._socket.onopened = this.socketopened.bind(this);
            this._socket.onmessage = this.socketmessage.bind(this);
            this._socket.onclosed = this.socketclosed.bind(this);
        }

        /**
         * Tritt auf, wenn die Verbindung zum Server erfolgreich hergestellt wurde.
         */
        private socketopened(ev: Common.ExWebSocket.WebSocketMessage): void
        {
            this._lastStateWasClosed = false;
            this._element.removeAttribute("connection");

            console.info("Verbindung hergestellt.", "Controller");

            for (let service of this._serviceEndpoints) {
                service.onopened(ev);
            }
        }

        /**
         * Tritt auf, wenn eine Nachricht vom Server empfangen wurde.
         */
        private socketmessage(ev: Common.ExWebSocket.WebSocketMessage): void
        {
            for (let service of this._serviceEndpoints) {
                service.onmessage(ev);
            }
        }

        /**
         * Tritt auf, wenn die Verbindung zum Server geschlossen wurde.
         */
        private socketclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void
        {
            this._element.setAttribute("connection", "");

            if (this._socket != null)
            {
                this._socket.onopened = null;
                this._socket.onmessage = null;
                this._socket.onclosed = null;
                this._socket = null;
            }

            if( ! this._lastStateWasClosed)
            {
                this._lastStateWasClosed = true;

                console.info(`Verbindung getrennt.`, "Controller");
    
                for (let service of this._serviceEndpoints) {
                    service.onclosed(ev);
                }
               
            }
            setTimeout(this.connect.bind(this), this._serviceTimeout);
        }
    }
}