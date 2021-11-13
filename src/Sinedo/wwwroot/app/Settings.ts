/// <reference path="./Services/NotificationControl.ts" />
/// <reference path="./Common/Control.ts" />

namespace Application {

    export class Settings {

        private static _setupMessage: string;
        private static _loginMessage: string;
        private static _errorMessage: string;

        /**
         * Beginnt die Anwendung zu laden.
         */
        public static Main() {
            Settings.loadLocalizedStrings();

            let controls: Array<HTMLInputElement> =
            [
                Common.Control.get("input_bandwidth"),
                Common.Control.get("input_concurrentDownloads"),
                Common.Control.get("input_downloadDirectory"),
                Common.Control.get("input_extractingDirectory"),
                Common.Control.get("input_isExtractingEnabled")
            ]

            controls.forEach(element => {
                element.addEventListener('change', Settings.onChanged);   
            });
        }

        private static onChanged(e: Event) {
            var element:HTMLInputElement = e.target as HTMLInputElement;
            var newValue: string;

            if (element.type === "checkbox") {
                newValue = element.checked ? "true" : "false";
            }
            else {
                newValue = element.value;
            }

            fetch("/api/settings?name=" + element.getAttribute("data-setting-name"), {
                method: "POST",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(newValue)
            }).then(response => {
                switch(response.status) {
                    case 200: {
                        break;
                    }
                    case 403: {
                        alert(Settings._setupMessage);
                        break;
                    }
                    case 401: {
                        alert(Settings._loginMessage);
                        break;
                    }
                    default: {
                        alert(Settings._errorMessage);
                        break;
                    }
                }
            });
        }

        private static loadLocalizedStrings(): void {
            // Übersetzung für Nachrichtenboxen auslesen.
            let template: HTMLTemplateElement = document.getElementById('localized_dialog_strings') as HTMLTemplateElement;
            let template_strings: HTMLTemplateElement = template.content.cloneNode(true) as HTMLTemplateElement;
            
            this._setupMessage = template_strings.children[0].textContent;
            this._loginMessage = template_strings.children[1].textContent;
            this._errorMessage = template_strings.children[2].textContent;
        }
    }
}