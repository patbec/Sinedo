/// <reference path="../Controller.ts" />
/// <reference path="../Common/ExWebSocket/WebSocketMessage.ts" />
/// <reference path="../Common/ExWebSocket/WebSocketCloseEvent.ts" />

namespace Application.Interfaces {

    export interface IServiceEndpoint {

        onactivate(ev: Application.Controller): void;
        onopened(ev: Common.ExWebSocket.WebSocketMessage): void;
        onmessage(ev: Common.ExWebSocket.WebSocketMessage): void;
        onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void;
    }
}