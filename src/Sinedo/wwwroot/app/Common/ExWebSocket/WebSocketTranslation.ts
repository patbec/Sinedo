/// <reference path="WebSocketCodes.ts" />

namespace Application.Common.ExWebSocket {

    export class WebSocketTranslation {

        public static NoSocketMessage = "Die Verbindung zum Server wurde ohne Nachricht beendet.";

        public static Socket: Map<number, string> = new Map<number, string>([
            [WebSocketCodes.CLOSE_GOING_AWAY,      "Der Server wurde heruntergefahren, der Dienst steht aktuell nicht zur Verfügung."],
            [WebSocketCodes.CLOSE_ABNORMAL,        "Es konnte keine Verbindung zu Ihrem Server hergestellt werden."],
            [WebSocketCodes.POLICY_VIOLATION,      "Die Verbindung wurde aufgrund eines Protokollfehlers geschlossen."],
            [WebSocketCodes.SERVER_ERROR,          "Die Verbindung wurde aufgrund eines Serverfehlers geschlossen."],
            [WebSocketCodes.CLOSED_BY_CLIENT,      "Die Verbindung wurde durch den Client geschlossen."],
            [WebSocketCodes.POLICY_VIOLATION_BY_CLIENT, "Die Verbindung wurde aufgrund eines Protokollfehlers beim Client geschlossen."]
        ]);
    }
}