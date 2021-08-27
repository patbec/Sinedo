/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="NotificationControl.ts" />

namespace Application.Services {

    export class NotificationEndpoint implements Interfaces.IServiceEndpoint {

        private _control: NotificationControl;
        private _controller: Controller;

        /**
         * Initialisiert das Benachrichtigungs-Steuerelement.
         */
        public onactivate(ev: Controller): void {
            this._controller = ev;
            this._control = NotificationControl.current;
        }

        /**
         * Bereitet das Benachrichtigungs-Steuerelement für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
        }

        /**
         * Verarbeitet einen eingehenden Befehl.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.Error) {
                let contract: Interfaces.ICommandMessage = ev.message;
                this._control.addException(contract.errorType);

                if (contract.messageLog != null)
                    console.warn("A server error has occurred " + contract.messageLog);
            }
        }

        /**
         * Bereinigt das Benachrichtigungs-Steuerelement.
         */
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {

        }
    }
}