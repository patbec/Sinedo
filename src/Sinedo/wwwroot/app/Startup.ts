/// <reference path="./Services/NotificationControl.ts" />
/// <reference path="./Services/DiskEndpoint.ts" />
/// <reference path="./Services/ListboxEndpoint.ts" />
/// <reference path="./Services/NotificationEndpoint.ts" />
/// <reference path="./Services/CardEndpoint.ts" />
/// <reference path="./Services/SystemEndpoint.ts" />
/// <reference path="./Services/DragAndDropEndpoint.ts" />

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


            let services: Array<Interfaces.IServiceEndpoint> =
            [
                new Application.Services.DiskEndpoint(),
                new Application.Services.ListboxEndpoint(),
                new Application.Services.NotificationEndpoint(),
                new Application.Services.CardEndpoint(),
                new Application.Services.SystemEndpoint(),
                new Application.Services.DragAndDropEndpoint(),
            ]

            this._controller = new Controller(services, 2000);

            // var n = new Application.Services.CardControl();
            // n.isEnabled = true;

            // var data: number[] = new Array(30);
            // data[0] = 50;
            // data[1] = 50;
            // data[2] = 50;
            // data[3] = 0;
            // data[4] = 50;
            // data[5] = 100;
            // data[8] = 100;
            // data[28] = 0;
            // data[29] = 100;

            // window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", ((ev: object) => {
            //     console.info("Changed");
            //     n.draw(data);
            // }).bind(this));

            // n.draw(data);

            // var element1: Application.Interfaces.IGroupRecord = { id: 1, name: "Test 1", state: Application.Flags.State.Running};
            // var element2: Application.Interfaces.IGroupRecord = { id: 2, name: "Test 2", state: Application.Flags.State.Completed};
            // var element3: Application.Interfaces.IGroupRecord = { id: 3, name: "Test 3", state: Application.Flags.State.Canceled};
            // var element4: Application.Interfaces.IGroupRecord = { id: 4, name: "Test 4", state: Application.Flags.State.Read};
            // var element5: Application.Interfaces.IGroupRecord = { id: 5, name: "Test 5", state: Application.Flags.State.Failed};

            // Startup.y = new Application.Services.ListboxControl();
            // Startup.y.onaction = Startup.callback.bind(this);

            // Startup.y.add(element1);
            // Startup.y.add(element2);
            // Startup.y.add(element3);
            // Startup.y.add(element4);
            // Startup.y.add(element5);
        }

        // private static callback(groupid: number, action: Flags.ClientCommands | number): void {
        //     if(action == Application.Flags.ClientCommands.Delete) {
        //         this.y.remove(groupid);
        //     }
        // }

    }
}