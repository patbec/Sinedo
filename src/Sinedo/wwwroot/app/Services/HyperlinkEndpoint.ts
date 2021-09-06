/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="HyperlinkControl.ts" />

namespace Application.Services {

    export class HyperlinkEndpoint implements Interfaces.IServiceEndpoint {

        private _control: HyperlinkControl;
        private _controller: Controller;

        /**
         * Initialisiert das Links-Steuerelement.
         */
        public onactivate(ev: Controller): void {
            this._controller = ev;
            this._control = new HyperlinkControl();
            this._control.onaction = this.onuseraction.bind(this);
        }

        /**
         * Bereitet das Links-Steuerelement für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.Setup) {
                let contract: Interfaces.ICommandSetup = ev.message;

                if(contract != null) {
                    // Steuerelement aktualisieren.
                    this._control.updateLinks(contract.links);
                }
            }
        }

        /**
         * Verarbeitet einen eingehenden Befehl.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.LinksChanged) {
                let contract: Interfaces.ICommandHyperlink[] = ev.message;
                this._control.updateLinks(contract);
            }
        }

        /**
         * Bereinigt das Links-Steuerelement.
         */
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {

        }

        private onuseraction(links: Application.Interfaces.ICommandHyperlink[]) {
            try {
                let result: boolean = this._controller.send(Application.Flags.ClientCommands.Links, 0, links);

                if ( ! result) {
                    throw new Error("The socket has refused to send data.");
                }
            } catch (e) {
                console.error("Action could not be executed: " + e);
                NotificationControl.current.addException("ClientCommandFailed");
            }
        }
    }
}