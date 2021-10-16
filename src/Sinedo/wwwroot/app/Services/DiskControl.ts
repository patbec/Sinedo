/// <reference path="../Common/Control.ts" />
/// <reference path="../Common/ByteSizes.ts" />
/// <reference path="CardControl.ts" />

namespace Application.Services {

    export class DiskControl {

        private _elementRoot: HTMLDivElement;
        private _elementOnline: HTMLDivElement;
        private _elementOffline: HTMLDivElement;
        private _elementCanvas: HTMLCanvasElement;

        private _labelFree: HTMLSpanElement;
        private _labelSize: HTMLSpanElement;

        private _context: CanvasContext;

        public constructor() {
            this._elementRoot = Common.Control.get("disk");
            this._elementOnline = Common.Control.get("diskOnline");
            this._elementOffline = Common.Control.get("diskOffline");
            this._elementCanvas = Common.Control.get("disk_canvas");

            this._labelFree = Common.Control.get("disk_free");
            this._labelSize = Common.Control.get("disk_size");

            this._context = new CanvasContext(this._elementCanvas);
        }

        public set status(online: boolean) {
            this._elementOffline.hidden = online;
            this._elementOnline.hidden = ! online;
        }

        /**
         * Schreibt die Anzahl an heruntergeladenen Bytes in das Steuerelement.
         * @value Die Anzahl von gelesenen Bytes in dieser Sitzung.
         */
        public set free(bytes: number) {
            this._labelFree.innerText = 
                Application.Common.ByteSizes.formatBytes(bytes, 0);
        }

        /**
         * Schreibt die Geschwindigkeit in das Steuerelement.
         * @value Die Anzahl von Bytes pro Sekunde.
         */
        public set size(bytes: number) {
            this._labelSize.innerText =
                Application.Common.ByteSizes.formatBytes(bytes, 0);
        }

        /**
         * Zeichnet die Auslastung.
         * @value Werte die gezeichnet werden sollen.
         */
        public draw(data: number[]) {
            this._context.draw(data);
        }
    }
}