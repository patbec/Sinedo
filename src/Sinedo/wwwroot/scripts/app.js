var Application;
(function (Application) {
    var Common;
    (function (Common) {
        var ExWebSocket;
        (function (ExWebSocket) {
            /**
             * Status Codes warum eine Verbindung zum Server getrennt wurde.
             **/
            let WebSocketCodes;
            (function (WebSocketCodes) {
                WebSocketCodes[WebSocketCodes["CLOSE_NORMAL"] = 1000] = "CLOSE_NORMAL";
                WebSocketCodes[WebSocketCodes["CLOSE_GOING_AWAY"] = 1001] = "CLOSE_GOING_AWAY";
                WebSocketCodes[WebSocketCodes["CLOSE_PROTOCOL_ERROR"] = 1002] = "CLOSE_PROTOCOL_ERROR";
                WebSocketCodes[WebSocketCodes["CLOSE_UNSUPPORTED"] = 1003] = "CLOSE_UNSUPPORTED";
                WebSocketCodes[WebSocketCodes["CLOSED_NO_STATUS"] = 1005] = "CLOSED_NO_STATUS";
                WebSocketCodes[WebSocketCodes["CLOSE_ABNORMAL"] = 1006] = "CLOSE_ABNORMAL";
                WebSocketCodes[WebSocketCodes["UNSUPPORTED_PAYLOAD"] = 1007] = "UNSUPPORTED_PAYLOAD";
                WebSocketCodes[WebSocketCodes["POLICY_VIOLATION"] = 1008] = "POLICY_VIOLATION";
                WebSocketCodes[WebSocketCodes["CLOSE_TOO_LARGE"] = 1009] = "CLOSE_TOO_LARGE";
                WebSocketCodes[WebSocketCodes["MANDATORY_EXTENSION"] = 1010] = "MANDATORY_EXTENSION";
                WebSocketCodes[WebSocketCodes["SERVER_ERROR"] = 1011] = "SERVER_ERROR";
                WebSocketCodes[WebSocketCodes["SERVICE_RESTART"] = 1012] = "SERVICE_RESTART";
                WebSocketCodes[WebSocketCodes["TRY_AGAIN_LATER"] = 1013] = "TRY_AGAIN_LATER";
                WebSocketCodes[WebSocketCodes["BAD_GATEWAY"] = 1014] = "BAD_GATEWAY";
                WebSocketCodes[WebSocketCodes["TLS_HANDSHAKE_FAIL"] = 1015] = "TLS_HANDSHAKE_FAIL";
                WebSocketCodes[WebSocketCodes["CLOSED_BY_CLIENT"] = 4100] = "CLOSED_BY_CLIENT";
                WebSocketCodes[WebSocketCodes["POLICY_VIOLATION_BY_CLIENT"] = 4101] = "POLICY_VIOLATION_BY_CLIENT";
            })(WebSocketCodes = ExWebSocket.WebSocketCodes || (ExWebSocket.WebSocketCodes = {}));
        })(ExWebSocket = Common.ExWebSocket || (Common.ExWebSocket = {}));
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
/// <reference path="WebSocketCodes.ts" />
var Application;
(function (Application) {
    var Common;
    (function (Common) {
        var ExWebSocket;
        (function (ExWebSocket) {
            class WebSocketCloseEvent {
                constructor(ev, successful) {
                    this.code = ev.code;
                    this.reason = ev.reason;
                    this.wasClean = ev.wasClean;
                    this.successful = successful;
                }
                static from(ev, successful) {
                    return new WebSocketCloseEvent(ev, successful);
                }
            }
            ExWebSocket.WebSocketCloseEvent = WebSocketCloseEvent;
        })(ExWebSocket = Common.ExWebSocket || (Common.ExWebSocket = {}));
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
var Application;
(function (Application) {
    var Flags;
    (function (Flags) {
        /**
         * Befehle die vom Client an den Server gesendet werden.
         */
        let ClientCommands;
        (function (ClientCommands) {
            ClientCommands[ClientCommands["Start"] = 2] = "Start";
            ClientCommands[ClientCommands["Stop"] = 3] = "Stop";
            ClientCommands[ClientCommands["Delete"] = 4] = "Delete";
            ClientCommands[ClientCommands["StartAll"] = 7] = "StartAll";
            ClientCommands[ClientCommands["StopAll"] = 8] = "StopAll";
            ClientCommands[ClientCommands["Upload"] = 9] = "Upload";
            ClientCommands[ClientCommands["Links"] = 10] = "Links";
        })(ClientCommands = Flags.ClientCommands || (Flags.ClientCommands = {}));
        /**
         * Befehle die vom Server an den Client gesendet werden.
         */
        let ServerCommands;
        (function (ServerCommands) {
            /**
             * Beim Verarbeiten eines Befehls ist ein Fehler aufgetreten.
             */
            ServerCommands[ServerCommands["Error"] = 1] = "Error";
            /**
             * Ein neues Objekt wurde hinzugefügt.
             */
            ServerCommands[ServerCommands["Added"] = 2] = "Added";
            /**
             * Ein Objekt wurde entfernt.
             */
            ServerCommands[ServerCommands["Removed"] = 3] = "Removed";
            /**
             * Der Status eines Objektes hat sich geändert.
             */
            ServerCommands[ServerCommands["Changed"] = 4] = "Changed";
            /**
             * Eine neue Verbindung wurde hergestellt, diese Nachricht enthält alle Daten.
             */
            ServerCommands[ServerCommands["Setup"] = 5] = "Setup";
            /**
             * Es ist eine Benachrichtigung verfügbar.
             */
            ServerCommands[ServerCommands["Notification"] = 6] = "Notification";
            /**
             * Es gibt neue Daten für das Monitoring.
             */
            ServerCommands[ServerCommands["Monitor"] = 9] = "Monitor";
            /**
             * Es gibt neue Daten für den Datenträger.
             */
            ServerCommands[ServerCommands["DiskInfo"] = 7] = "DiskInfo";
            ServerCommands[ServerCommands["BandwidthInfo"] = 8] = "BandwidthInfo";
            ServerCommands[ServerCommands["LinksChanged"] = 9] = "LinksChanged";
        })(ServerCommands = Flags.ServerCommands || (Flags.ServerCommands = {}));
    })(Flags = Application.Flags || (Application.Flags = {}));
})(Application || (Application = {}));
/// <reference path="WebSocketCodes.ts" />
/// <reference path="../../Flags/Commands.ts" />
var Application;
(function (Application) {
    var Common;
    (function (Common) {
        var ExWebSocket;
        (function (ExWebSocket) {
            class WebSocketMessage {
                constructor(command, parameter, message) {
                    this.command = command;
                    this.parameter = parameter;
                    this.message = message;
                }
                /**
                 * Erstellt aus den angegebenen Werten ein Paket, dass an den Server gesendet werden kann.
                 **/
                getbytes() {
                    let buffer = null;
                    let bufferView = null;
                    ;
                    try {
                        if (this.message != null) {
                            let contentViewer = new TextEncoder();
                            let contentBytes;
                            contentBytes = contentViewer.encode(JSON.stringify(this.message));
                            let base = new Uint8Array(contentBytes.length + 2);
                            base.set(contentBytes, 2);
                            buffer = base.subarray(0, contentBytes.length + 2).buffer;
                        }
                        else {
                            buffer = new ArrayBuffer(2);
                        }
                        bufferView = new DataView(buffer);
                        bufferView.setInt8(0, this.command);
                        bufferView.setInt8(1, this.parameter);
                        return bufferView.buffer;
                    }
                    catch (e) {
                        throw new Error("Creation of a WebSocket package failed: " + e);
                    }
                }
                /**
                 * Erstellt auf den angegebenen Bytes ein Event-Paket.
                 * @param buffer Das vom Server empfangene WebSocket-Datenpaket.
                 */
                static parse(buffer) {
                    let bufferView = new DataView(buffer);
                    let command = bufferView.getInt8(0);
                    let parameter = bufferView.getInt8(1);
                    let message = null;
                    /* Prüfen ob das Paket einen Inhalt besitzt. */
                    if (bufferView.byteLength > 2) {
                        /* Byte-Array aus dem Inhalt erstellen. */
                        let data = new Uint8Array(bufferView.buffer, 2, bufferView.buffer.byteLength - 2);
                        /* Byte-Array zu einer Zeichenfolge konvertieren. */
                        let content = new TextDecoder().decode(data);
                        message = JSON.parse(content);
                    }
                    return new WebSocketMessage(command, parameter, message);
                }
            }
            ExWebSocket.WebSocketMessage = WebSocketMessage;
        })(ExWebSocket = Common.ExWebSocket || (Common.ExWebSocket = {}));
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
/// <reference path="WebSocketCloseEvent.ts" />
/// <reference path="WebSocketCodes.ts" />
/// <reference path="WebSocketMessage.ts" />
var Application;
(function (Application) {
    var Common;
    (function (Common) {
        var ExWebSocket;
        (function (ExWebSocket) {
            class WebSocketEndpoint {
                constructor(url) {
                    this._connectionSuccessful = false;
                    this._connectionSocket = new WebSocket(url);
                    this._connectionSocket.binaryType = 'arraybuffer';
                    this._connectionSocket.addEventListener('message', this.socketmessage.bind(this));
                    this._connectionSocket.addEventListener('close', this.socketclose.bind(this));
                }
                get isopen() {
                    return this._connectionSocket.readyState == WebSocket.OPEN;
                }
                /**
                 * Schließt die Verbindung mit dem angegebenen Code.
                 */
                close(code, reason) {
                    if (this.isopen) {
                        this._connectionSocket.close(code, reason);
                    }
                }
                /**
                 * Sendet einen Befehl an den Server.
                 */
                send(message) {
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
                socketmessage(ev) {
                    try {
                        let message = ExWebSocket.WebSocketMessage.parse(ev.data);
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
                        this._connectionSocket.close(ExWebSocket.WebSocketCodes.POLICY_VIOLATION_BY_CLIENT, e); // FixMe: Return always 1009
                    }
                }
                /**
                 * Tritt auf, wenn die Verbindung zum Server geschlossen wurde.
                 */
                socketclose(ev) {
                    try {
                        this.onclosed(ExWebSocket.WebSocketCloseEvent.from(ev, this._connectionSuccessful));
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
                static getaddress(hostname, port, protocol) {
                    let suffix;
                    if (protocol == "https:") {
                        suffix = "wss:";
                    }
                    else {
                        suffix = "ws:";
                    }
                    return `${suffix}//${hostname}:${port}/api/server-connetion.ws?version=1`;
                }
            }
            ExWebSocket.WebSocketEndpoint = WebSocketEndpoint;
        })(ExWebSocket = Common.ExWebSocket || (Common.ExWebSocket = {}));
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
/// <reference path="WebSocketCodes.ts" />
var Application;
(function (Application) {
    var Common;
    (function (Common) {
        var ExWebSocket;
        (function (ExWebSocket) {
            class WebSocketTranslation {
            }
            WebSocketTranslation.NoSocketMessage = "Die Verbindung zum Server wurde ohne Nachricht beendet.";
            WebSocketTranslation.Socket = new Map([
                [ExWebSocket.WebSocketCodes.CLOSE_GOING_AWAY, "Der Server wurde heruntergefahren, der Dienst steht aktuell nicht zur Verfügung."],
                [ExWebSocket.WebSocketCodes.CLOSE_ABNORMAL, "Es konnte keine Verbindung zu Ihrem Server hergestellt werden."],
                [ExWebSocket.WebSocketCodes.POLICY_VIOLATION, "Die Verbindung wurde aufgrund eines Protokollfehlers geschlossen."],
                [ExWebSocket.WebSocketCodes.SERVER_ERROR, "Die Verbindung wurde aufgrund eines Serverfehlers geschlossen."],
                [ExWebSocket.WebSocketCodes.CLOSED_BY_CLIENT, "Die Verbindung wurde durch den Client geschlossen."],
                [ExWebSocket.WebSocketCodes.POLICY_VIOLATION_BY_CLIENT, "Die Verbindung wurde aufgrund eines Protokollfehlers beim Client geschlossen."]
            ]);
            ExWebSocket.WebSocketTranslation = WebSocketTranslation;
        })(ExWebSocket = Common.ExWebSocket || (Common.ExWebSocket = {}));
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
/// <reference path="../Controller.ts" />
/// <reference path="../Common/ExWebSocket/WebSocketMessage.ts" />
/// <reference path="../Common/ExWebSocket/WebSocketCloseEvent.ts" />
/// <reference path="Common/ExWebSocket/WebSocketEndpoint.ts" />
/// <reference path="Common/ExWebSocket/WebSocketMessage.ts" />
/// <reference path="Common/ExWebSocket/WebSocketCloseEvent.ts" />
/// <reference path="Common/ExWebSocket/WebSocketTranslation.ts" />
/// <reference path="Interfaces/IServiceEndpoint.ts" />
/// <reference path="Flags/Commands.ts" />
var Application;
(function (Application) {
    /**
     * Controller um mit dem Server zu kommunizieren.
     */
    class Controller {
        /**
         * Erstellt einen neuen Controller mit den angegebenen Diensten.
         * @param endpoints Endpunkte um Nachrichten zu verarbeiten.
         * @param timeout Wartezeit in ms bis ein erneuter Verbindungsversuch gestartet wird.
         */
        constructor(endpoints, timeout = 5000) {
            this._lastStateWasClosed = true;
            this._element = Application.Common.Control.get("body");
            this._serviceAddress = Application.Common.ExWebSocket.WebSocketEndpoint.getaddress(window.location.hostname, window.location.port, window.location.protocol);
            this._serviceEndpoints = endpoints;
            this._serviceTimeout = timeout;
            for (let service of this._serviceEndpoints) {
                // Auch nach Fehlern beim Aktivieren vom Diensten versuchen zu verbinden.
                // Falls Fehler von einem unwichtigen Dienst kommen kann die Anwendung trotzdem verwendet werden. 
                try {
                    service.onactivate(this);
                }
                catch (e) {
                    window.alert("Error on service.onactivate: " + e);
                }
            }
            this.connect();
        }
        /**
         * Gibt einen Wert zurück, wie lange gewartet werden soll
         * bis ein erneuter Verbindungsversuch gestartet wird.
         */
        get timeout() {
            return this._serviceTimeout;
        }
        /**
         * Sendet einen Befehl an den Server.
         * Es wird True zurückgegeben, wenn der Befehl erfolgreich gesendet wurde.
         * @param command
         * @param parameter
         * @param content
         */
        send(command, parameter, content) {
            let message = new Application.Common.ExWebSocket.WebSocketMessage(command, parameter, content);
            return this._socket.send(message);
        }
        /**
         * Versucht eine neue Verbindung herzustellen.
         */
        connect() {
            this._socket = new Application.Common.ExWebSocket.WebSocketEndpoint(this._serviceAddress);
            this._socket.onopened = this.socketopened.bind(this);
            this._socket.onmessage = this.socketmessage.bind(this);
            this._socket.onclosed = this.socketclosed.bind(this);
        }
        /**
         * Tritt auf, wenn die Verbindung zum Server erfolgreich hergestellt wurde.
         */
        socketopened(ev) {
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
        socketmessage(ev) {
            for (let service of this._serviceEndpoints) {
                service.onmessage(ev);
            }
        }
        /**
         * Tritt auf, wenn die Verbindung zum Server geschlossen wurde.
         */
        socketclosed(ev) {
            this._element.setAttribute("connection", "");
            if (this._socket != null) {
                this._socket.onopened = null;
                this._socket.onmessage = null;
                this._socket.onclosed = null;
                this._socket = null;
            }
            if (!this._lastStateWasClosed) {
                this._lastStateWasClosed = true;
                console.info(`Verbindung getrennt.`, "Controller");
                for (let service of this._serviceEndpoints) {
                    service.onclosed(ev);
                }
            }
            setTimeout(this.connect.bind(this), this._serviceTimeout);
        }
    }
    Application.Controller = Controller;
})(Application || (Application = {}));
class Debug {
    /**
     * Dieser Code kann mit 'Debug.Show()' über die Browser-Console aufgerufen werden.
     */
    static Show() {
        var snackbar = Application.Services.NotificationControl.current;
        snackbar.addException("Test");
        snackbar.addException("Test");
        snackbar.addException("Test");
        return true;
    }
}
var Application;
(function (Application) {
    var Common;
    (function (Common) {
        class Control {
            static get(name) {
                if (name == null || name == "")
                    throw Error(`The ''${name}'' parameter must not be empty.`);
                let control = document.getElementById(name);
                if (control == null)
                    throw Error(`The control with the name '${name}' was not found`);
                let result = control;
                if (result == null)
                    throw Error(`The control element '${name}' has an incorrect type.`);
                return result;
            }
        }
        Common.Control = Control;
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
/// <reference path="../Common/Control.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class NotificationControl {
            constructor() {
                this._messages = new Array();
                this._isRunning = false;
                this._elementSnackbar = Application.Common.Control.get("snackbar");
                this._elementSnackbar.addEventListener("animationend", ((ev) => {
                    if (ev.target == this._elementSnackbar) {
                        this.endShowMessage();
                    }
                }).bind(this));
                this._elementSnackbar.addEventListener("pointermove", this.pauseAnimation.bind(this));
                this._elementSnackbar.addEventListener("pointerenter", this.pauseAnimation.bind(this));
                this._elementSnackbar.addEventListener("pointerleave", this.playAnimation.bind(this));
            }
            static get current() {
                if (this._current == null)
                    this._current = new NotificationControl();
                return this._current;
            }
            /**
             * Pausiert die Animation wenn mit der Maus darüber gefahren wird.
             */
            pauseAnimation(ev) {
                // Nicht pausieren wenn die Animation gerade beim ein- oder ausblenden ist.
                let opacity = window.getComputedStyle(this._elementSnackbar).opacity;
                if (opacity == "1") {
                    this._elementSnackbar.classList.add("paused");
                }
            }
            /**
             * Setzt die Animation wieder fort.
             */
            playAnimation(ev) {
                this._elementSnackbar.classList.remove("paused");
            }
            /**
             * Eine Nachricht zur Warteschlange Hinzufügen und Anzeigen.
             * @param message Nachricht
             */
            addException(exception) {
                if (exception == null || exception == "") {
                    console.warn("Notification ignored.");
                    return;
                }
                this._messages.push(exception);
                if (!this._isRunning) {
                    this.beginShowMessage();
                }
            }
            /**
             * Beginnt eine Nachricht aus dem Stapel List<string> anzuzeigen.
             **/
            beginShowMessage() {
                this._isRunning = true;
                let exeptionType = this._messages.shift();
                let exceptionList = this._elementSnackbar.firstElementChild.children;
                let exceptionMessageFound = false;
                // Attribute mit dem Fehlertyp vergleichen, bei Übereinstimmung das jeweilige Text-Element mit der übersetzten Fehlermeldung anzeigen.
                for (let index = 0; index < exceptionList.length; index++) {
                    const element = exceptionList[index];
                    // Fehlertyp vergleichen.
                    if (element.getAttribute("data-exception") == exeptionType) {
                        element.hidden = false;
                        exceptionMessageFound = true;
                    }
                    else {
                        element.hidden = true;
                    }
                }
                // Falls keine übersetzte Fehlermeldung gefunden wurde, den Fehlertyp anzeigen.
                if (!exceptionMessageFound) {
                    exceptionList[0].textContent = exeptionType;
                    exceptionList[0].hidden = false;
                }
                this._elementSnackbar.setAttribute("open", "");
            }
            /**
             * Zeigt die nächste Nachricht an oder beendet die Ausführung.
             **/
            endShowMessage() {
                this._elementSnackbar.removeAttribute("open");
                /* Zeigt die nächste Nachricht an oder beendet die Ausführung. */
                if (this._messages.length != 0) {
                    setTimeout(this.beginShowMessage.bind(this), 400);
                }
                else {
                    this._isRunning = false;
                }
            }
        }
        Services.NotificationControl = NotificationControl;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
var Application;
(function (Application) {
    var Flags;
    (function (Flags) {
        let State;
        (function (State) {
            /**
             * Der Vorgang wurde hinzugefügt aber nicht gestartet.
             */
            State[State["Idle"] = 1] = "Idle";
            /**
             * Der Vorgang befindet sich in der Warteschlange.
             */
            State[State["Queued"] = 2] = "Queued";
            /**
             * Der Vorgang wurde durch den Benutzer abgebrochen.
             */
            State[State["Canceled"] = 3] = "Canceled";
            /**
             * Der Vorgang wurde durch einen Fehler abgebrochen.
             */
            State[State["Failed"] = 4] = "Failed";
            /**
             * Der Vorgang wird vom Aufgabenplaner ausgeführt.
             */
            State[State["Running"] = 5] = "Running";
            /**
             * Der Vorgang wurde erfolgreich abgeschlossen.
             */
            State[State["Completed"] = 6] = "Completed";
            /**
             * Der Vorgang wird entfernt.
             */
            State[State["Deleting"] = 8] = "Deleting";
            /**
             * Der Vorgang wird angehalten.
             */
            State[State["Stopping"] = 9] = "Stopping";
            /**
             * Die Eingabedatei wird nicht unterstützt.
             */
            State[State["Unsupported"] = 10] = "Unsupported";
        })(State = Flags.State || (Flags.State = {}));
    })(Flags = Application.Flags || (Application.Flags = {}));
})(Application || (Application = {}));
var Application;
(function (Application) {
    var Flags;
    (function (Flags) {
        let Meta;
        (function (Meta) {
            Meta[Meta["CheckStatus"] = 0] = "CheckStatus";
            Meta[Meta["Download"] = 1] = "Download";
            Meta[Meta["Retry"] = 2] = "Retry";
            Meta[Meta["Extract"] = 3] = "Extract";
        })(Meta = Flags.Meta || (Flags.Meta = {}));
    })(Flags = Application.Flags || (Application.Flags = {}));
})(Application || (Application = {}));
/// <reference path="../Flags/State.ts" />
/// <reference path="../Flags/Meta.ts" />
var Application;
(function (Application) {
    var Common;
    (function (Common) {
        class ByteSizes {
            /**
             * Source: https://stackoverflow.com/a/18650828
             */
            static formatBytes(bytes, decimals = 2) {
                if (bytes === 0)
                    return '0 Bytes';
                const k = 1024;
                const dm = decimals < 0 ? 0 : decimals;
                const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
                const i = Math.floor(Math.log(bytes) / Math.log(k));
                return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
            }
        }
        Common.ByteSizes = ByteSizes;
    })(Common = Application.Common || (Application.Common = {}));
})(Application || (Application = {}));
/// <reference path="../Common/Control.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class CanvasContext {
            /**
             * Erstellt einen neue Renderer für die Breitbandauslastung.
             */
            constructor(element) {
                this._canvas = element;
                window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", ((ev) => {
                    this.draw(this._lastData);
                }).bind(this));
            }
            /**
             * Zeichnet die Auslastung.
             * @value Array mit Werten.
             */
            draw(value, clear = true) {
                this.resizeCanvasToDisplaySize(this._canvas);
                let canvas = this._canvas;
                if (canvas.width != 0) {
                    var themeColor = getComputedStyle(document.documentElement).getPropertyValue('--color-theme-primary');
                    let context = canvas.getContext('2d');
                    context.fillStyle = themeColor;
                    context.strokeStyle = themeColor;
                    let lineCount = CanvasContext.HISTORY_LENGTH - 1;
                    let lineWidth = canvas.width / lineCount;
                    let lineHeight = canvas.height;
                    /* Für die Redraw-Funktion cachen */
                    this._lastData = value;
                    /* Beginn des Pfades zeichnen */
                    context.beginPath();
                    context.moveTo(0, lineHeight);
                    /* Auslastung zeichnen */
                    for (var i = 0; i <= lineCount; i++) {
                        var percent = value[i] * canvas.height / 100;
                        context.lineTo(lineWidth * i, lineHeight - percent);
                    }
                    /* Ende des Pfades zeichnen */
                    context.lineTo(canvas.width, canvas.height);
                    /* Zeichnen abschließen */
                    context.fill();
                    context.stroke();
                    context.closePath();
                }
            }
            resizeCanvasToDisplaySize(canvas) {
                // Lookup the size the browser is displaying the canvas in CSS pixels.
                const displayWidth = canvas.clientWidth;
                const displayHeight = canvas.clientHeight;
                // Check if the canvas is not the same size.
                const needResize = canvas.width !== displayWidth ||
                    canvas.height !== displayHeight;
                if (needResize) {
                    let dpr = window.devicePixelRatio || 1;
                    // Make the canvas the same size
                    canvas.width = displayWidth * dpr;
                    canvas.height = displayHeight * dpr;
                }
                return needResize;
            }
        }
        CanvasContext.HISTORY_LENGTH = 30;
        Services.CanvasContext = CanvasContext;
        class CardControl {
            constructor() {
                this._elementRoot = Application.Common.Control.get("card");
                this._elementCanvas = Application.Common.Control.get("card_canvas");
                this._labelSummary = Application.Common.Control.get("card_summary");
                this._labelDownload = Application.Common.Control.get("card_download");
                this._context = new CanvasContext(this._elementCanvas);
            }
            /**
             * Schreibt die Anzahl an heruntergeladenen Bytes in das Steuerelement.
             * @value Die Anzahl von gelesenen Bytes in dieser Sitzung.
             */
            set totalBytes(bytes) {
                this._labelSummary.innerText =
                    Application.Common.ByteSizes.formatBytes(bytes, 2);
            }
            /**
             * Schreibt die Geschwindigkeit in das Steuerelement.
             * @value Die Anzahl von Bytes pro Sekunde.
             */
            set currentBytes(bytes) {
                this._labelDownload.innerText =
                    Application.Common.ByteSizes.formatBytes(bytes, 2);
            }
            /**
             * Zeichnet die Auslastung.
             * @value Werte die gezeichnet werden sollen.
             */
            draw(data) {
                this._context.draw(data);
            }
        }
        Services.CardControl = CardControl;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Common/Control.ts" />
/// <reference path="../Common/ByteSizes.ts" />
/// <reference path="CardControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class DiskControl {
            constructor() {
                this._elementRoot = Application.Common.Control.get("disk");
                this._elementOnline = Application.Common.Control.get("diskOnline");
                this._elementOffline = Application.Common.Control.get("diskOffline");
                this._elementCanvas = Application.Common.Control.get("disk_canvas");
                this._labelFree = Application.Common.Control.get("disk_free");
                this._labelSize = Application.Common.Control.get("disk_size");
                this._context = new Services.CanvasContext(this._elementCanvas);
            }
            set status(online) {
                this._elementOffline.hidden = online;
                this._elementOnline.hidden = !online;
            }
            /**
             * Schreibt die Anzahl an heruntergeladenen Bytes in das Steuerelement.
             * @value Die Anzahl von gelesenen Bytes in dieser Sitzung.
             */
            set free(bytes) {
                this._labelFree.innerText =
                    Application.Common.ByteSizes.formatBytes(bytes, 0);
            }
            /**
             * Schreibt die Geschwindigkeit in das Steuerelement.
             * @value Die Anzahl von Bytes pro Sekunde.
             */
            set size(bytes) {
                this._labelSize.innerText =
                    Application.Common.ByteSizes.formatBytes(bytes, 0);
            }
            /**
             * Zeichnet die Auslastung.
             * @value Werte die gezeichnet werden sollen.
             */
            draw(data) {
                this._context.draw(data);
            }
        }
        Services.DiskControl = DiskControl;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="DiskControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class DiskEndpoint {
            constructor() {
                this._lastDeviceState = true;
            }
            /**
             * Initialisiert das Bandbreitensteuerelement.
             */
            onactivate(ev) {
                this._control = new Services.DiskControl();
            }
            /**
             * Bereitet das Speicherplatzsteuerelement für die Verwendung vor.
             */
            onopened(ev) {
                if (ev.command == Application.Flags.ServerCommands.Setup) {
                    let contract = ev.message;
                    if (contract.diskInfo != null) {
                        // Steuerelemente aktualisieren.
                        this._lastDeviceState = true;
                        this.update(contract.diskInfo);
                    }
                }
            }
            /**
             * Fügt einen neuen Wert zum Speicherplatzsteuerelement hinzu.
             */
            onmessage(ev) {
                if (ev.command == Application.Flags.ServerCommands.DiskInfo) {
                    let contract = ev.message;
                    // Steuerelemente aktualisieren.
                    this.update(contract);
                }
            }
            update(diskInfo) {
                if (diskInfo.isAvailable) {
                    // Steuerelemente einrichten.
                    this._control.free = diskInfo.freeBytes;
                    this._control.size = diskInfo.totalSize;
                    // Auslastung zeichnen.
                    this._control.draw(diskInfo.data);
                    if (this._lastDeviceState != true) {
                        this._lastDeviceState = true;
                        this._control.status = true;
                    }
                }
                else {
                    // Auslastung mit leeren Werten überschreiben.
                    this._control.draw(new Array(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
                    if (this._lastDeviceState != false) {
                        this._lastDeviceState = false;
                        this._control.status = false;
                    }
                }
            }
            /**
             * Bereinigt das Speicherplatzsteuerelement.
             */
            onclosed(ev) {
            }
        }
        Services.DiskEndpoint = DiskEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/State.ts" />
var Application;
(function (Application) {
    var Converter;
    (function (Converter) {
        class Icon {
            /**
             * Konvertiert den angegebenen Status zu einem Symbol.
             */
            static Convert(value) {
                let group = value;
                switch (group.state) {
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
        Converter.Icon = Icon;
    })(Converter = Application.Converter || (Application.Converter = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="../Converter/Icon.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class ListboxControl {
            constructor() {
                this._element = Application.Common.Control.get("listbox");
                this._placeholder = Application.Common.Control.get("placeholder");
                this._scheduler = Application.Common.Control.get("scheduler_icon");
                this._collection = new Map();
                this._cache = new Map();
                // Register Buttons
                let buttonScheduler = Application.Common.Control.get("scheduler");
                let buttonAltStart = Application.Common.Control.get("button_start");
                let buttonAltStop = Application.Common.Control.get("button_stop");
                this.loadLocalizedStrings();
                buttonScheduler.addEventListener("click", ((e) => {
                    if (!this._wasDownloadActive) {
                        this.onstop();
                    }
                    else {
                        this.onstart();
                    }
                }).bind(this));
                buttonAltStart.addEventListener("click", ((e) => {
                    this.onstart();
                }).bind(this));
                buttonAltStop.addEventListener("click", ((e) => {
                    this.onstop();
                }).bind(this));
            }
            loadLocalizedStrings() {
                // Übersetzung für Nachrichtenboxen auslesen.
                let template = document.getElementById('localized_dialog_strings');
                let template_strings = template.content.cloneNode(true);
                this._dialogMessage = template_strings.children[0].textContent;
            }
            add(item) {
                let element = ListboxControl.cloneItem();
                element.id = "listitem-" + item.name;
                element.title = item.name;
                element.setAttribute(ListboxControl.ATTR_1, String(item.name));
                element.setAttribute(ListboxControl.ATTR_2, String(item.state));
                element.addEventListener("click", this.onuseraction.bind(this));
                element.children[0].textContent = Application.Converter.Icon.Convert(item);
                element.children[1].textContent = item.name;
                ListboxControl.setDescription(element, item);
                ListboxControl.setCommandFlags(element, item);
                if (this._collection.has(item.name)) {
                    this._element.replaceChild(this._collection[item.name], element);
                }
                else {
                    this._element.appendChild(element);
                }
                this._collection.set(item.name, element);
                this._cache.set(item.name, item);
            }
            onuseraction(event) {
                let source = event.target;
                if (source != null && !source.hasAttribute("command")) {
                    source = source.parentElement;
                }
                if (source != null && source.hasAttribute("command")) {
                    let attributeFolderName = source
                        .parentElement
                        .parentElement.getAttribute(ListboxControl.ATTR_1);
                    let attributeCommand = Number.parseInt(source.getAttribute("command"));
                    let raiseEvent = true;
                    if (attributeCommand == Application.Flags.ClientCommands.Delete) {
                        raiseEvent = confirm(this._dialogMessage);
                    }
                    if (raiseEvent) {
                        this.onaction(attributeFolderName, attributeCommand);
                    }
                }
            }
            change(item) {
                if (this._collection.has(item.name)) {
                    this._cache.set(item.name, item);
                    let element = this._collection.get(item.name);
                    element.setAttribute(ListboxControl.ATTR_2, String(item.state));
                    // Das aktuelle Icon beim Löschen behalten.
                    // Verhindert ein kurzes Flackern des Icons wenn der Download gelöscht wird.
                    if (item.state != Application.Flags.State.Deleting) {
                        element.children[0].textContent = Application.Converter.Icon.Convert(item);
                    }
                    ListboxControl.setDescription(element, item);
                    ListboxControl.setCommandFlags(element, item);
                }
            }
            remove(name) {
                if (this._collection.has(name)) {
                    this.removeItemSafe(this._collection.get(name));
                    this._collection.delete(name);
                    this._cache.delete(name);
                }
            }
            clear() {
                while (this._element.children.length != 1) {
                    this.removeItemSafe(this._element.children[1]);
                }
                this._collection.clear();
                this._cache.clear();
            }
            removeItemSafe(item) {
                item.removeEventListener("click", this.onuseraction.bind(this));
                this._element.removeChild(item);
            }
            static cloneItem() {
                let template = document.getElementById('listitem_template');
                let template_child = template.content.firstElementChild.cloneNode(true);
                if (template_child == null) {
                    throw Error("Failed to create a listboxitem from the template.");
                }
                return template_child;
            }
            updateSchedulerIcon() {
                let activeDownloads = 0;
                this._cache.forEach(download => {
                    if (download.state == Application.Flags.State.Queued ||
                        download.state == Application.Flags.State.Running) {
                        activeDownloads++;
                    }
                });
                let isDownloadActive = (activeDownloads == 0);
                // Reduzierung von Neuzeichnen in Browsern.
                if (this._wasDownloadActive != isDownloadActive) {
                    this._wasDownloadActive = isDownloadActive;
                    if (activeDownloads == 0) {
                        this._scheduler.innerText = ListboxControl.SchedulerIcon_Start;
                    }
                    else {
                        this._scheduler.innerText = ListboxControl.SchedulerIcon_Stop;
                    }
                }
            }
            updatePlaceholder() {
                this._placeholder.hidden = (this._collection.size != 0);
            }
            static setDescription(element, torrent) {
                switch (torrent.state) {
                    case Application.Flags.State.Running: {
                        this.onStatusRunning(element, torrent);
                        break;
                    }
                    case Application.Flags.State.Failed: {
                        this.onStatusFailed(element, torrent);
                        break;
                    }
                }
            }
            static onStatusRunning(element, download) {
                let elementDownload = element.getElementsByClassName("download")[0];
                let elementRetry = element.getElementsByClassName("retry")[0];
                let elementCheckStatus = element.getElementsByClassName("checkStatus")[0];
                elementDownload.hidden = true;
                elementRetry.hidden = true;
                elementCheckStatus.hidden = true;
                switch (download.meta) {
                    case Application.Flags.Meta.CheckStatus: {
                        elementCheckStatus.hidden = false;
                        break;
                    }
                    case Application.Flags.Meta.Retry: {
                        elementRetry.hidden = false;
                        break;
                    }
                    case Application.Flags.Meta.Extract: {
                        elementDownload.children[1].hidden = false;
                    }
                    case Application.Flags.Meta.Download: {
                        elementDownload.children[0].hidden = false;
                    }
                    case Application.Flags.Meta.Extract:
                    case Application.Flags.Meta.Download: {
                        elementDownload.hidden = false;
                        let labelDownload = elementDownload.children[0];
                        let labelExtract = elementDownload.children[1];
                        // Not perfect but ok.
                        if (download.meta == Application.Flags.Meta.Download) {
                            labelDownload.hidden = false;
                            labelExtract.hidden = true;
                        }
                        else {
                            labelDownload.hidden = true;
                            labelExtract.hidden = false;
                        }
                        let elementPercent = elementDownload.getElementsByClassName("infoPercent")[0];
                        let elementTime = elementDownload.getElementsByClassName("infoTime")[0];
                        let elementSpeed = elementDownload.getElementsByClassName("infoSpeed")[0];
                        if (download.groupPercent != null) {
                            elementPercent.hidden = false;
                            elementPercent.firstChild.textContent = String(download.groupPercent);
                        }
                        else {
                            elementPercent.hidden = true;
                        }
                        if (download.secondsToComplete != null && download.secondsToComplete != 0) {
                            elementTime.hidden = false;
                            elementTime.firstChild.textContent = String(Math.round(download.secondsToComplete / 60));
                        }
                        else {
                            elementTime.hidden = true;
                        }
                        if (download.bytesPerSecond != null) {
                            elementSpeed.hidden = false;
                            elementSpeed.firstChild.textContent = Application.Common.ByteSizes.formatBytes(download.bytesPerSecond, 2);
                        }
                        else {
                            elementSpeed.hidden = true;
                        }
                    }
                }
            }
            static onStatusFailed(element, download) {
                let elementException = element.getElementsByClassName("infoError")[0];
                let exceptionList = elementException.children;
                let exceptionMessageFound = false;
                // Attribute mit dem Fehlertyp vergleichen, bei Übereinstimmung das jeweilige Text-Element mit der übersetzten Fehlermeldung anzeigen.
                for (let index = 0; index < exceptionList.length; index++) {
                    const element = exceptionList[index];
                    // Fehlertyp vergleichen.
                    if (element.getAttribute("data-exception") == download.lastException) {
                        element.hidden = false;
                        exceptionMessageFound = true;
                    }
                    else {
                        element.hidden = true;
                    }
                }
                // Falls keine übersetzte Fehlermeldung gefunden wurde, den Fehlertyp anzeigen.
                if (!exceptionMessageFound) {
                    exceptionList[0].textContent = download.lastException;
                    exceptionList[0].hidden = false;
                }
            }
            static setCommandFlags(element, torrent) {
                var buttons = element.children[3].children;
                var buttonStart = buttons[0];
                var buttonCancel = buttons[1];
                var buttonRetry = buttons[2];
                var buttonRemove = buttons[3];
                let state = torrent.state;
                // Kann Vorgang hinzufügen.
                buttonStart.hidden = !(state == Application.Flags.State.Idle);
                // Kann Vorgang abbrechen.
                buttonCancel.hidden = !(state == Application.Flags.State.Running ||
                    state == Application.Flags.State.Queued);
                // Kann Vorgang wiederholen.
                buttonRetry.hidden = !(state == Application.Flags.State.Failed ||
                    state == Application.Flags.State.Completed ||
                    state == Application.Flags.State.Canceled);
                // Kann Vorgang löschen.
                buttonRemove.hidden = !(state == Application.Flags.State.Idle ||
                    state == Application.Flags.State.Failed ||
                    state == Application.Flags.State.Completed ||
                    state == Application.Flags.State.Canceled);
            }
        }
        ListboxControl.ATTR_1 = "data-group";
        ListboxControl.ATTR_2 = "data-state";
        ListboxControl.SchedulerIcon_Start = "play_arrow";
        ListboxControl.SchedulerIcon_Stop = "pause";
        Services.ListboxControl = ListboxControl;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="./NotificationControl.ts" />
/// <reference path="./ListboxControl.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class ListboxEndpoint {
            /**
             * Initialisiert das Listbox-Steuerelement.
             */
            onactivate(ev) {
                this._controller = ev;
                this._control = new Services.ListboxControl();
                this._control.onaction = this.onuseraction.bind(this);
                this._control.onstart = this.onstartaction.bind(this);
                this._control.onstop = this.onstopaction.bind(this);
            }
            /**
             * Bereitet das Listbox-Steuerelement für die Verwendung vor.
             */
            onopened(ev) {
                if (ev.command == Application.Flags.ServerCommands.Setup) {
                    let contract = ev.message;
                    this._control.clear();
                    contract.downloads.forEach(item => this._control.add(item));
                    this._control.updateSchedulerIcon();
                    this._control.updatePlaceholder();
                }
            }
            /**
             * Verarbeitet einen eingehenden Befehl.
             */
            onmessage(ev) {
                switch (ev.command) {
                    case Application.Flags.ServerCommands.Added: {
                        let torrent = ev.message;
                        this._control.add(torrent);
                        this._control.updateSchedulerIcon();
                        this._control.updatePlaceholder();
                        break;
                    }
                    case Application.Flags.ServerCommands.Changed: {
                        let torrent = ev.message;
                        this._control.change(torrent);
                        this._control.updateSchedulerIcon();
                        break;
                    }
                    case Application.Flags.ServerCommands.Removed: {
                        this._control.remove(ev.message);
                        this._control.updateSchedulerIcon();
                        this._control.updatePlaceholder();
                        break;
                    }
                }
            }
            /**
             * Bereinigt das Listbox-Steuerelement.
             */
            onclosed(ev) {
            }
            onuseraction(folderName, action) {
                try {
                    if (folderName == undefined || folderName == null || folderName == "") {
                        throw new Error("The group has no valid identification name.");
                    }
                    // Only allow StartAll, StartOrRetry, Cancel, Delete;
                    switch (action) {
                        case Application.Flags.ClientCommands.Start:
                        case Application.Flags.ClientCommands.Stop:
                        case Application.Flags.ClientCommands.Delete: {
                            break;
                        }
                        default:
                            throw new Error("The flag is invalid for this element.");
                    }
                    let result = this._controller.send(action, 0, { "name": folderName });
                    if (!result) {
                        throw new Error("The socket has refused to send data.");
                    }
                }
                catch (e) {
                    console.error("Action could not be executed: " + e);
                    Services.NotificationControl.current.addException("ClientCommandFailed");
                }
            }
            onstartaction() {
                try {
                    let result = this._controller.send(Application.Flags.ClientCommands.StartAll, 0, null);
                    if (!result) {
                        throw new Error("The socket has refused to send data.");
                    }
                }
                catch (e) {
                    console.error("Action could not be executed: " + e);
                    Services.NotificationControl.current.addException("ClientCommandFailed");
                }
            }
            onstopaction() {
                try {
                    let result = this._controller.send(Application.Flags.ClientCommands.StopAll, 0, null);
                    if (!result) {
                        throw new Error("The socket has refused to send data.");
                    }
                }
                catch (e) {
                    console.error("Action could not be executed: " + e);
                    Services.NotificationControl.current.addException("ClientCommandFailed");
                }
            }
        }
        Services.ListboxEndpoint = ListboxEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="NotificationControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class NotificationEndpoint {
            /**
             * Initialisiert das Benachrichtigungs-Steuerelement.
             */
            onactivate(ev) {
                this._controller = ev;
                this._control = Services.NotificationControl.current;
            }
            /**
             * Bereitet das Benachrichtigungs-Steuerelement für die Verwendung vor.
             */
            onopened(ev) {
            }
            /**
             * Verarbeitet einen eingehenden Befehl.
             */
            onmessage(ev) {
                if (ev.command == Application.Flags.ServerCommands.Error) {
                    let contract = ev.message;
                    this._control.addException(contract.errorType);
                    if (contract.messageLog != null)
                        console.warn("A server error has occurred " + contract.messageLog);
                }
            }
            /**
             * Bereinigt das Benachrichtigungs-Steuerelement.
             */
            onclosed(ev) {
            }
        }
        Services.NotificationEndpoint = NotificationEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="CardControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class CardEndpoint {
            /**
             * Initialisiert das Bandbreitensteuerelement.
             */
            onactivate(ev) {
                this._control = new Services.CardControl();
            }
            /**
             * Bereitet das Speicherplatzsteuerelement für die Verwendung vor.
             */
            onopened(ev) {
                if (ev.command == Application.Flags.ServerCommands.Setup) {
                    let contract = ev.message;
                    if (contract.bandwidthInfo != null) {
                        // Steuerelemente aktualisieren.
                        this.update(contract.bandwidthInfo);
                    }
                }
            }
            /**
             * Fügt einen neuen Wert zum Speicherplatzsteuerelement hinzu.
             */
            onmessage(ev) {
                if (ev.command == Application.Flags.ServerCommands.BandwidthInfo) {
                    let contract = ev.message;
                    // Steuerelemente aktualisieren.
                    this.update(contract);
                }
            }
            update(bandwidthInfo) {
                // Steuerelemente einrichten.
                this._control.totalBytes = bandwidthInfo.bytesReadTotal;
                this._control.currentBytes = bandwidthInfo.bytesRead;
                // Auslastung zeichnen.
                this._control.draw(bandwidthInfo.data);
            }
            /**
             * Bereinigt das Bandbreitensteuerelement.
             */
            onclosed(ev) {
            }
        }
        Services.CardEndpoint = CardEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class SystemControl {
            constructor() {
            }
            updateData(hostname, platform, arch, pid, version) {
                document.getElementsByName("info_hostname")
                    .forEach(e => e.innerText = hostname);
                document.getElementsByName("info_platform")
                    .forEach(e => e.innerText = platform.toString());
                document.getElementsByName("info_arch")
                    .forEach(e => e.innerText = arch);
                document.getElementsByName("info_pid")
                    .forEach(e => e.innerText = pid.toString());
                document.getElementsByName("info_version")
                    .forEach(e => e.innerText = version);
            }
        }
        Services.SystemControl = SystemControl;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="SystemControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class SystemEndpoint {
            /**
             * Initialisiert das .
             */
            onactivate(ev) {
                this._control = new Services.SystemControl();
            }
            /**
             * Bereitet das  für die Verwendung vor.
             */
            onopened(ev) {
                if (ev.command == Application.Flags.ServerCommands.Setup) {
                    let contract = ev.message;
                    this._control.updateData(contract.systemInfo.hostname, contract.systemInfo.platform, contract.systemInfo.architecture, contract.systemInfo.pid, contract.systemInfo.version);
                }
            }
            /**
             * Fügt einen neuen Wert zum  hinzu.
             */
            onmessage(ev) {
            }
            /**
             * Bereinigt das .
             */
            onclosed(ev) {
            }
        }
        Services.SystemEndpoint = SystemEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="NotificationControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class DragAndDropEndpoint {
            /**
             * Initialisiert das Benachrichtigungs-Steuerelement.
             */
            onactivate(ev) {
                this._controller = ev;
                window.addEventListener("dragover", this.DragOverEventHandler.bind(this), false);
                window.addEventListener("drop", this.DropEventHandler.bind(this), false);
            }
            /**
             * Bereitet das Benachrichtigungs-Steuerelement für die Verwendung vor.
             */
            onopened(ev) {
                this._fileSupportEnabled = true;
            }
            /**
             * Verarbeitet einen eingehenden Befehl.
             */
            onmessage(ev) {
            }
            /**
             * Bereinigt das Benachrichtigungs-Steuerelement.
             */
            onclosed(ev) {
                this._fileSupportEnabled = false;
            }
            DragOverEventHandler(evt) {
                evt.stopPropagation();
                evt.preventDefault();
                if (this._fileSupportEnabled == true)
                    evt.dataTransfer.dropEffect = "move";
                else
                    evt.dataTransfer.dropEffect = "none";
            }
            DropEventHandler(evt) {
                evt.stopPropagation();
                evt.preventDefault();
                if (this._fileSupportEnabled == false)
                    return;
                try {
                    /* Die hinzugefügten Dateien prüfen und an den Server senden. */
                    for (let file of evt.dataTransfer.files) {
                        let reader;
                        let controller = this._controller;
                        if (DragAndDropEndpoint.CheckFile(file)) {
                            reader = new FileReader();
                            reader.onload = function () {
                                let content = {
                                    fileName: file.name,
                                    files: reader.result.toString().split("\n"),
                                    autostart: true
                                };
                                // Wrapper um eine Eingabedatei an den Server zu senden.
                                let result = controller.send(Application.Flags.ClientCommands.Upload, 0, content);
                                if (!result) {
                                    console.error(`The file '${file.name}' could not be uploaded.`);
                                    Services.NotificationControl.current.addException("UploadFailed");
                                }
                            };
                            reader.readAsText(file);
                        }
                        else {
                            console.error(`The file '${file.name}' exceeds the maximum allowed size.`);
                            Services.NotificationControl.current.addException("UploadSizeExceeded");
                        }
                    }
                }
                catch (e) {
                    console.error("The files could not be uploaded to the server:" + e);
                    Services.NotificationControl.current.addException("UploadFailed");
                }
            }
            /**
             * Prüft die Dateigröße.
             */
            static CheckFile(file) {
                return file.size < 8655000;
            }
        }
        Services.DragAndDropEndpoint = DragAndDropEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Common/Control.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class HyperlinkControl {
            constructor() {
                this._elementRoot = Application.Common.Control.get("links");
                this._elementRootShow = this._elementRoot.children[1];
                this._elementRootEdit = this._elementRoot.children[2];
                this._elementLinkBox = Application.Common.Control.get("linkbox");
                this._elementLinkEdit = Application.Common.Control.get("linkedit");
                this._buttonEdit = Application.Common.Control.get("buttonLinksEdit");
                this._buttonSave = Application.Common.Control.get("buttonLinksSave");
                this._buttonCancel = Application.Common.Control.get("buttonLinksCancel");
                this._buttonEdit.addEventListener("click", ((e) => {
                    this.updateEditMode();
                    this.editMode = true;
                }).bind(this));
                this._buttonCancel.addEventListener("click", ((e) => {
                    this.clearEditMode();
                    this.editMode = false;
                }).bind(this));
                this._buttonSave.addEventListener("click", ((e) => {
                    this.sendNewLinks();
                    this.editMode = false;
                }).bind(this));
            }
            set editMode(value) {
                this._elementRootShow.hidden = value;
                this._elementRootEdit.hidden = !value;
            }
            /**
             * Setzt die angegebenen Links.
             * Während das Steuerelement bearbeitet wird, werden keine neuen Links angezeigt.
             * @value
             */
            updateLinks(links) {
                let linkBox = this._elementLinkBox;
                for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                    let listElement = linkBox.children[index];
                    let linkElement = listElement.firstElementChild;
                    let textPrimary = linkElement.firstElementChild;
                    let textSecondary = linkElement.lastElementChild;
                    if (links[index] != null) {
                        listElement.hidden = false;
                        // Link zuweisen.
                        linkElement.href = links[index].url;
                        textPrimary.textContent = links[index].displayName;
                        textSecondary.textContent = links[index].url;
                    }
                    else {
                        listElement.hidden = true;
                        textPrimary.textContent = "";
                        textSecondary.textContent = "";
                        linkElement.removeAttribute("href");
                    }
                }
            }
            /**
             * Aktualisiert die Eingabesteuerelemente.
             * @value
             */
            updateEditMode() {
                let linkBox = this._elementLinkBox;
                let linkEdit = this._elementLinkEdit;
                for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                    let listElement = linkBox.children[index];
                    let linkElement = listElement.firstElementChild;
                    let editDiv = linkEdit.children[index].firstElementChild;
                    let editFieldDisplayName = editDiv.firstElementChild;
                    let editFieldUrl = editDiv.lastElementChild;
                    if (listElement.hidden == false) {
                        editFieldDisplayName.value = linkElement.firstElementChild.textContent;
                        editFieldUrl.value = linkElement.href;
                    }
                    else {
                        editFieldDisplayName.value = "";
                        editFieldUrl.value = "";
                    }
                }
            }
            /**
             * Bereinigt die Eingabesteuerelemente.
             * @value
             */
            clearEditMode() {
                let linkEdit = this._elementLinkEdit;
                for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                    let editDiv = linkEdit.children[index].firstElementChild;
                    let editFieldDisplayName = editDiv.firstElementChild;
                    let editFieldUrl = editDiv.lastElementChild;
                    editFieldDisplayName.value = "";
                    editFieldUrl.value = "";
                }
            }
            /**
             * Sendet die Eingaben an den Server.
             * @value
             */
            sendNewLinks() {
                let links = [null, null, null];
                let linkEdit = this._elementLinkEdit;
                for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                    let editDiv = linkEdit.children[index].firstElementChild;
                    let editFieldDisplayName = editDiv.firstElementChild;
                    let editFieldUrl = editDiv.lastElementChild;
                    links[index] =
                        {
                            displayName: editFieldDisplayName.value,
                            url: editFieldUrl.value,
                        };
                }
                this.onaction(links);
            }
        }
        HyperlinkControl.MAX_LINKS = 3;
        Services.HyperlinkControl = HyperlinkControl;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="HyperlinkControl.ts" />
var Application;
(function (Application) {
    var Services;
    (function (Services) {
        class HyperlinkEndpoint {
            /**
             * Initialisiert das Links-Steuerelement.
             */
            onactivate(ev) {
                this._controller = ev;
                this._control = new Services.HyperlinkControl();
                this._control.onaction = this.onuseraction.bind(this);
            }
            /**
             * Bereitet das Links-Steuerelement für die Verwendung vor.
             */
            onopened(ev) {
                if (ev.command == Application.Flags.ServerCommands.Setup) {
                    let contract = ev.message;
                    if (contract != null) {
                        // Steuerelement aktualisieren.
                        this._control.updateLinks(contract.links);
                    }
                }
            }
            /**
             * Verarbeitet einen eingehenden Befehl.
             */
            onmessage(ev) {
                if (ev.command == Application.Flags.ServerCommands.LinksChanged) {
                    let contract = ev.message;
                    this._control.updateLinks(contract);
                }
            }
            /**
             * Bereinigt das Links-Steuerelement.
             */
            onclosed(ev) {
            }
            onuseraction(links) {
                try {
                    let result = this._controller.send(Application.Flags.ClientCommands.Links, 0, links);
                    if (!result) {
                        throw new Error("The socket has refused to send data.");
                    }
                }
                catch (e) {
                    console.error("Action could not be executed: " + e);
                    Services.NotificationControl.current.addException("ClientCommandFailed");
                }
            }
        }
        Services.HyperlinkEndpoint = HyperlinkEndpoint;
    })(Services = Application.Services || (Application.Services = {}));
})(Application || (Application = {}));
/// <reference path="./Services/NotificationControl.ts" />
/// <reference path="./Services/DiskEndpoint.ts" />
/// <reference path="./Services/ListboxEndpoint.ts" />
/// <reference path="./Services/NotificationEndpoint.ts" />
/// <reference path="./Services/CardEndpoint.ts" />
/// <reference path="./Services/SystemEndpoint.ts" />
/// <reference path="./Services/DragAndDropEndpoint.ts" />
/// <reference path="./Services/HyperlinkEndpoint.ts" />
var Application;
(function (Application) {
    class Startup {
        /* Globaler Verweis auf die aktuelle Controller-Instanz. */
        static get Controller() {
            return this._controller;
        }
        /**
         * Beginnt die Anwendung zu laden.
         */
        static Main() {
            console.log(`Welcome to
   _____ _                __      
  / ___/(_)___  ___  ____/ /___   
  \\__ \\/ / __ \\/ _ \\/ __  / __ \\  
 ___/ / / / / /  __/ /_/ / /_/ /     
/____/_/_/ /_/\\___/\\__,_/\\____/   

Your Simple Network Downloader!
https://github.com/patbec/Sinedo
`);
            let services = [
                new Application.Services.DiskEndpoint(),
                new Application.Services.ListboxEndpoint(),
                new Application.Services.NotificationEndpoint(),
                new Application.Services.CardEndpoint(),
                new Application.Services.SystemEndpoint(),
                new Application.Services.DragAndDropEndpoint(),
                new Application.Services.HyperlinkEndpoint(),
            ];
            this._controller = new Application.Controller(services, 2000);
        }
    }
    Application.Startup = Startup;
})(Application || (Application = {}));
//# sourceMappingURL=app.js.map