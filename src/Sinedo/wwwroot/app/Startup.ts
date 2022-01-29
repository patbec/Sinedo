/// <reference path="./Services/NotificationControl.ts" />
/// <reference path="./Services/DiskEndpoint.ts" />
/// <reference path="./Services/ListboxEndpoint.ts" />
/// <reference path="./Services/NotificationEndpoint.ts" />
/// <reference path="./Services/CardEndpoint.ts" />
/// <reference path="./Services/SystemEndpoint.ts" />
/// <reference path="./Services/DragAndDropEndpoint.ts" />
/// <reference path="./Services/HyperlinkEndpoint.ts" />

namespace Application {

    export class Startup {

        private static _controller: Controller;

        /* Globaler Verweis auf die aktuelle Controller-Instanz. */
        public static get Controller(): Controller {
            return this._controller;
        }

        /**
         * Beginnt die Anwendung zu laden.
         */
        public static Main() {

            console.log(`Welcome to
   _____ _                __      
  / ___/(_)___  ___  ____/ /___   
  \\__ \\/ / __ \\/ _ \\/ __  / __ \\  
 ___/ / / / / /  __/ /_/ / /_/ /     
/____/_/_/ /_/\\___/\\__,_/\\____/   

Your Simple Network Downloader!
https://github.com/patbec/Sinedo
`);

            try {
                let services: Array<Interfaces.IServiceEndpoint> =
                    [
                        new Application.Services.DiskEndpoint(),
                        new Application.Services.ListboxEndpoint(),
                        new Application.Services.NotificationEndpoint(),
                        new Application.Services.CardEndpoint(),
                        new Application.Services.SystemEndpoint(),
                        new Application.Services.DragAndDropEndpoint(),
                        new Application.Services.HyperlinkEndpoint(),
                    ]

                this._controller = new Controller(services, 2000);
            }
            catch (e) {
                alert("Sorry, an error occurred while loading the WebApp: " + e)
            }
        }
    }
}