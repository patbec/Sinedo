namespace Application.Common.ExWebSocket {
    /**
     * Status Codes warum eine Verbindung zum Server getrennt wurde. 
     **/
    export enum WebSocketCodes {
        CLOSE_NORMAL = 1000,
        CLOSE_GOING_AWAY = 1001,
        CLOSE_PROTOCOL_ERROR = 1002,
        CLOSE_UNSUPPORTED = 1003,
        CLOSED_NO_STATUS = 1005,
        CLOSE_ABNORMAL = 1006,
        UNSUPPORTED_PAYLOAD = 1007,
        POLICY_VIOLATION = 1008,
        CLOSE_TOO_LARGE = 1009,
        MANDATORY_EXTENSION = 1010,
        SERVER_ERROR = 1011,
        SERVICE_RESTART = 1012,
        TRY_AGAIN_LATER = 1013,
        BAD_GATEWAY = 1014,
        TLS_HANDSHAKE_FAIL = 1015,

        CLOSED_BY_CLIENT = 4100,
        POLICY_VIOLATION_BY_CLIENT = 4101,
    }
}