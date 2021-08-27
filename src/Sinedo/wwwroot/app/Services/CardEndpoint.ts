/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="CardControl.ts" />

namespace Application.Services {

    export class CardEndpoint implements Interfaces.IServiceEndpoint {

        private _control: CardControl;

        /**
         * Initialisiert das Bandbreitensteuerelement.
         */
        public onactivate(ev: Controller): void {
            this._control = new CardControl(); 
        }

        /**
         * Bereitet das Speicherplatzsteuerelement für die Verwendung vor.
         */
        public onopened(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.Setup) {
                let contract: Interfaces.ICommandSetup = ev.message;

                if(contract.bandwidthInfo != null) {
                    // Steuerelemente aktualisieren.
                    this.update(contract.bandwidthInfo);
                }
            }
        }

        /**
         * Fügt einen neuen Wert zum Speicherplatzsteuerelement hinzu.
         */
        public onmessage(ev: Common.ExWebSocket.WebSocketMessage): void {
            if (ev.command == Flags.ServerCommands.BandwidthInfo) {
                let contract: Interfaces.ICommandBandwidth = ev.message;

                // Steuerelemente aktualisieren.
                this.update(contract);
            }
        }

        private update(bandwidthInfo: Interfaces.ICommandBandwidth) {
            // Steuerelemente einrichten.
            this._control.totalBytes = bandwidthInfo.bytesReadTotal;
            this._control.currentBytes = bandwidthInfo.bytesRead;

            // Auslastung zeichnen.
            this._control.draw(bandwidthInfo.data);
        }

        /**
         * Bereinigt das Bandbreitensteuerelement.
         */
        public onclosed(ev: Common.ExWebSocket.WebSocketCloseEvent): void {

        }
    }
}