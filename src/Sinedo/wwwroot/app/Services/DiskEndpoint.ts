/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="DiskControl.ts" />

namespace Application.Services {

    export class DiskEndpoint implements Interfaces.IServiceEndpoint {

        private _control: DiskControl;

        /**
         * Initialisiert das Bandbreitensteuerelement.
         */
        public onactivate(ev: Controller): void {
            this._control = new DiskControl(); 
        }

        /**
         * Bereitet das Speicherplatzsteuerelement für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.Setup) {
                let contract: Interfaces.ICommandSetup = ev.message;

                if(contract.diskInfo != null) {
                    // Steuerelemente aktualisieren.
                    this.update(contract.diskInfo);
                }
            }
        }

        /**
         * Fügt einen neuen Wert zum Speicherplatzsteuerelement hinzu.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.DiskInfo) {
                let contract: Interfaces.ICommandDiskSpace = ev.message;

                // Steuerelemente aktualisieren.
                this.update(contract);
            }
        }

        private update(diskInfo: Interfaces.ICommandDiskSpace) {

            if(diskInfo.isAvailable) {
                // Steuerelemente einrichten.
                
                this._control.free = diskInfo.freeBytes;
                this._control.size = diskInfo.totalSize;

                // Auslastung zeichnen.
                this._control.draw(diskInfo.data);
                this._control.status = true;
            } else {
                // Auslastung mit leeren Werten überschreiben.
                this._control.draw(new Array(
                    0,0,0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,0,0)
                );

                this._control.status = false;
            }
        }

        /**
         * Bereinigt das Speicherplatzsteuerelement.
         */
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {

        }
    }
}