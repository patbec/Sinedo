/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="SystemControl.ts" />

namespace Application.Services {

    export class SystemEndpoint implements Interfaces.IServiceEndpoint {

        private _control: SystemControl;

        /**
         * Initialisiert das .
         */
        public onactivate(ev: Controller): void {
            this._control = new SystemControl(); 
        }

        /**
         * Bereitet das  für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.Setup) {
                let contract: Interfaces.ICommandSetup = ev.message;

                this._control.updateData(
                    contract.systemInfo.hostname,
                    contract.systemInfo.platform,
                    contract.systemInfo.architecture,
                    contract.systemInfo.pid,
                    contract.systemInfo.version);
            }
        }

        /**
         * Fügt einen neuen Wert zum  hinzu.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {

        }

        /**
         * Bereinigt das .
         */
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {

        }
    }
}