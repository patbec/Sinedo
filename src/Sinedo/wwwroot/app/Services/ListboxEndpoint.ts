/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="./NotificationControl.ts" />
/// <reference path="./ListboxControl.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />

namespace Application.Services {

    export class ListboxEndpoint implements Interfaces.IServiceEndpoint {

        private _control: ListboxControl;
        private _controller: Controller;

        /**
         * Initialisiert das Listbox-Steuerelement.
         */
        public onactivate(ev: Controller): void {
            this._controller = ev;
            this._control = new ListboxControl();
            this._control.onaction = this.onuseraction.bind(this);
            this._control.onstart = this.onstartaction.bind(this);
            this._control.onstop = this.onstopaction.bind(this);
        }

        /**
         * Bereitet das Listbox-Steuerelement für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.Setup) {
                let contract: Interfaces.ICommandSetup = ev.message;

                this._control.clear();
                contract.downloads.forEach(item => this._control.add(item));
                this._control.updateSchedulerIcon();
                this._control.updatePlaceholder();
            }
        }

        /**
         * Verarbeitet einen eingehenden Befehl.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {
            switch (ev.command) {
                case Flags.ServerCommands.Added: {
                    let torrent: Interfaces.IDownloadRecord = ev.message;

                    this._control.add(torrent);
                    this._control.updateSchedulerIcon();
                    this._control.updatePlaceholder();
                    break;
                }
                case Flags.ServerCommands.Changed: {
                    let torrent: Interfaces.IDownloadRecord = ev.message;

                    this._control.change(torrent);
                    this._control.updateSchedulerIcon();
                    break;
                }
                case Flags.ServerCommands.Removed: {
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
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {

        }

        private onuseraction(folderName: string, action: Flags.ClientCommands | number): void {
            try {
                if (folderName == undefined || folderName == null || folderName == "") {
                    throw new Error("The group has no valid identification name.");
                }
                // Only allow StartAll, StartOrRetry, Cancel, Delete;
                switch (action) {
                    case Flags.ClientCommands.Start:
                    case Flags.ClientCommands.Stop:
                    case Flags.ClientCommands.Delete: {
                        break;
                    }
                    default:
                        throw new Error("The flag is invalid for this element.");
                }
                
                let result: boolean = this._controller.send(action, 0, { "name": folderName } );

                if ( ! result) {
                    throw new Error("The socket has refused to send data.");
                }
            } catch (e) {
                console.error("Action could not be executed: " + e);
                NotificationControl.current.addException("ClientCommandFailed");
            }
        }

        private onstartaction(): void {
            try {
                let result: boolean = this._controller.send(Application.Flags.ClientCommands.StartAll, 0, null);

                if ( ! result) {
                    throw new Error("The socket has refused to send data.");
                }
            } catch (e) {
                console.error("Action could not be executed: " + e);
                NotificationControl.current.addException("ClientCommandFailed");
            }
        }

        private onstopaction(): void {
            try {
                let result: boolean = this._controller.send(Application.Flags.ClientCommands.StopAll, 0, null);

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