/// <reference path="WebSocketCodes.ts" />

namespace Application.Common.ExWebSocket {

    export class WebSocketCloseEvent {

        public readonly code: number | WebSocketCodes;
        public readonly reason: string;
        public readonly wasClean: boolean;
        public readonly successful: boolean;

        public constructor(ev: CloseEvent, successful: boolean) {
            this.code = ev.code;
            this.reason = ev.reason;
            this.wasClean = ev.wasClean;
            this.successful = successful;
        }

        public static from(ev: CloseEvent, successful: boolean) {
            return new WebSocketCloseEvent(ev, successful);
        }
    }
}