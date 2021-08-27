/// <reference path="../Common/Control.ts" />

namespace Application.Services {

    export class NotificationControl {

        private readonly _elementSnackbar: HTMLDivElement;

        private _messages = new Array<string>();
        private _isRunning = false;

        private static _current: NotificationControl;

        public static get current(): NotificationControl {
            if (this._current == null)
                this._current = new NotificationControl();

            return this._current;
        }

        public constructor() {
            this._elementSnackbar = Common.Control.get("snackbar");
            this._elementSnackbar.addEventListener("animationend", ((ev: AnimationEvent) => {
                if(ev.target == this._elementSnackbar) {
                    this.endShowMessage();
                }
            }).bind(this));
            this._elementSnackbar.addEventListener("pointermove", this.pauseAnimation.bind(this));
            this._elementSnackbar.addEventListener("pointerenter", this.pauseAnimation.bind(this));
            this._elementSnackbar.addEventListener("pointerleave", this.playAnimation.bind(this));
        }

        /**
         * Pausiert die Animation wenn mit der Maus darüber gefahren wird.
         */
        private pauseAnimation(ev: object): void {
            // Nicht pausieren wenn die Animation gerade beim ein- oder ausblenden ist.
            let opacity = window.getComputedStyle(this._elementSnackbar).opacity;
            if(opacity == "1") {
                this._elementSnackbar.classList.add("paused");
            }
        }
        /**
         * Setzt die Animation wieder fort.
         */
        private playAnimation(ev: object): void {
            this._elementSnackbar.classList.remove("paused");
        }

        /**
         * Eine Nachricht zur Warteschlange Hinzufügen und Anzeigen.
         * @param message Nachricht
         */
        public addException(exception: string): void {
            if (exception == null || exception == "") {
                console.warn("Notification ignored.");
                return;
            }

            this._messages.push(exception);

            if ( ! this._isRunning) {
                this.beginShowMessage();
            }
        }

        /**
         * Beginnt eine Nachricht aus dem Stapel List<string> anzuzeigen.
         **/
        private beginShowMessage() {
            this._isRunning = true;

            let exeptionType = this._messages.shift();
            let exceptionList = this._elementSnackbar.firstElementChild.children;
            let exceptionMessageFound: boolean = false;

            // Attribute mit dem Fehlertyp vergleichen, bei Übereinstimmung das jeweilige Text-Element mit der übersetzten Fehlermeldung anzeigen.
            for (let index = 0; index < exceptionList.length; index++) {
                const element: HTMLSpanElement = exceptionList[index] as HTMLSpanElement;

                // Fehlertyp vergleichen.
                if(element.getAttribute("data-exception") == exeptionType) {
                    element.hidden = false;
                    exceptionMessageFound = true;
                } else {
                    element.hidden = true;
                }
            }

            // Falls keine übersetzte Fehlermeldung gefunden wurde, den Fehlertyp anzeigen.
            if( ! exceptionMessageFound) {
                exceptionList[0].textContent = exeptionType;
                (exceptionList[0] as HTMLSpanElement).hidden = false;
            }
            this._elementSnackbar.setAttribute("open", "");
        }

        /**
         * Zeigt die nächste Nachricht an oder beendet die Ausführung.
         **/
        private endShowMessage() {
            this._elementSnackbar.removeAttribute("open");
            
            /* Zeigt die nächste Nachricht an oder beendet die Ausführung. */
            if (this._messages.length != 0) {
                setTimeout(this.beginShowMessage.bind(this), 400);
            }
            else {
                this._isRunning = false;
            }
        }
    }
}