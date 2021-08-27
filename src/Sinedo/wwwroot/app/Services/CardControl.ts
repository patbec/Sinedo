/// <reference path="../Common/Control.ts" />

namespace Application.Services {

    export class CanvasContext {

        private _canvas: HTMLCanvasElement;
        private _lastData: number[];

        private static HISTORY_LENGTH: number = 30;

        /**
         * Erstellt einen neue Renderer für die Breitbandauslastung.
         */
        public constructor(element: HTMLCanvasElement) {
            this._canvas = element;
        
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", ((ev: object) => {
                this.draw(this._lastData);
            }).bind(this));
        }

        /**
         * Zeichnet die Auslastung.
         * @value Array mit Werten.
         */
        public draw(value: number[], clear: boolean = true): void {

            this.resizeCanvasToDisplaySize(this._canvas);

            
            let canvas = this._canvas;

            if(canvas.width != 0) {
                var themeColor = getComputedStyle(document.documentElement).getPropertyValue('--color-theme-primary');

                let context = canvas.getContext('2d');
                context.fillStyle = themeColor;
                context.strokeStyle = themeColor;

                let lineCount = CanvasContext.HISTORY_LENGTH - 1;
                let lineWidth = canvas.width / lineCount;
                let lineHeight = canvas.height;

                /* Für die Redraw-Funktion cachen */
                this._lastData = value;

                /* Beginn des Pfades zeichnen */
                context.beginPath();
                context.moveTo(0, lineHeight);

                /* Auslastung zeichnen */
                for (var i = 0; i <= lineCount; i++) {
                    var percent =  value[i] * canvas.height / 100;
                    context.lineTo(lineWidth * i, lineHeight - percent);
                }

                /* Ende des Pfades zeichnen */
                context.lineTo(canvas.width,
                               canvas.height);

                /* Zeichnen abschließen */
                context.fill();
                context.stroke();
                context.closePath();            
            }
        }

        private resizeCanvasToDisplaySize(canvas: HTMLCanvasElement) {
            // Lookup the size the browser is displaying the canvas in CSS pixels.
            const displayWidth  = canvas.clientWidth;
            const displayHeight = canvas.clientHeight;
   
            // Check if the canvas is not the same size.
            const needResize = canvas.width  !== displayWidth ||
                               canvas.height !== displayHeight;
   
            if (needResize) {
                let dpr = window.devicePixelRatio || 1;

              // Make the canvas the same size
              canvas.width  = displayWidth * dpr;
              canvas.height = displayHeight * dpr;
            }
   
            return needResize;
        }
    }

    export class CardControl {

        private _elementRoot: HTMLDivElement;
        private _elementCanvas: HTMLCanvasElement;

        private _labelSummary: HTMLSpanElement;
        private _labelDownload: HTMLSpanElement;

        private _context: CanvasContext;

        public constructor() {
            this._elementRoot = Common.Control.get("card");
            this._elementCanvas = Common.Control.get("card_canvas");

            this._labelSummary = Common.Control.get("card_summary");
            this._labelDownload = Common.Control.get("card_download");

            this._context = new CanvasContext(this._elementCanvas);
        }

        /**
         * Schreibt die Anzahl an heruntergeladenen Bytes in das Steuerelement.
         * @value Die Anzahl von gelesenen Bytes in dieser Sitzung.
         */
        public set totalBytes(bytes: number) {
            this._labelSummary.innerText = 
                Application.Common.ByteSizes.formatBytes(bytes, 2);
        }

        /**
         * Schreibt die Geschwindigkeit in das Steuerelement.
         * @value Die Anzahl von Bytes pro Sekunde.
         */
        public set currentBytes(bytes: number) {
            this._labelDownload.innerText =
                Application.Common.ByteSizes.formatBytes(bytes, 2);
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