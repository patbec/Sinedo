/// <reference path="../Interfaces/IServiceEndpoint.ts" />
/// <reference path="../Interfaces/ICommand.ts" />
/// <reference path="../Flags/Commands.ts" />
/// <reference path="../Common/Control.ts" />
/// <reference path="../Controller.ts" />
/// <reference path="../Converter/Icon.ts" />

namespace Application.Services {

    export class ListboxControl {

        private _element: HTMLUListElement;
        private _placeholder: HTMLDivElement;
        private _scheduler: HTMLButtonElement;
        private _collection: Map<string, HTMLLIElement>;
        private _cache: Map<string, Interfaces.IDownloadRecord>;

        private _wasDownloadActive: boolean;

        private static ATTR_1 = "data-group";
        private static ATTR_2 = "data-state";

        private _dialogMessage: string;

        public onaction: ((this: ListboxControl, folderName: string, action: Flags.ClientCommands | number) => any) | null;

        public onstart: ((this: ListboxControl) => any) | null;
        public onstop: ((this: ListboxControl) => any) | null;

        public constructor() {
            this._element = Common.Control.get("listbox");
            this._placeholder = Common.Control.get("placeholder");
            this._scheduler = Common.Control.get("scheduler_icon");
            this._collection = new Map<string, HTMLLIElement>();
            this._cache = new Map<string, Interfaces.IDownloadRecord>();

            // Register Buttons
            let buttonScheduler: HTMLButtonElement = Common.Control.get("scheduler");
            let buttonAltStart: HTMLButtonElement = Common.Control.get("button_start"); 
            let buttonAltStop: HTMLButtonElement = Common.Control.get("button_stop");

            this.loadLocalizedStrings();

            buttonScheduler.addEventListener("click", ((e: any) => {
                if( ! this._wasDownloadActive) {
                    this.onstop();
                } else {
                    this.onstart();
                }
            }).bind(this));

            buttonAltStart.addEventListener("click", ((e: any) => {
                this.onstart();
            }).bind(this));

            buttonAltStop.addEventListener("click", ((e: any) => {
                this.onstop();
            }).bind(this));
        }

        private loadLocalizedStrings(): void {
            // Übersetzung für Nachrichtenboxen auslesen.
            let template: HTMLTemplateElement = document.getElementById('localized_dialog_strings') as HTMLTemplateElement;
            let template_strings: HTMLTemplateElement = template.content.cloneNode(true) as HTMLTemplateElement;
            
            this._dialogMessage = template_strings.children[0].textContent;
        }

        public add(item: Interfaces.IDownloadRecord) {
            let element: HTMLLIElement = ListboxControl.cloneItem();

            element.id = "listitem-" + item.name;
            element.title = item.name;

            element.setAttribute(ListboxControl.ATTR_1, String(item.name));
            element.setAttribute(ListboxControl.ATTR_2, String(item.state));

            element.addEventListener("click", this.onuseraction.bind(this));

            element.children[0].textContent = Converter.Icon.Convert(item);
            element.children[1].textContent = item.name;

            ListboxControl.setDescription(element, item);
            ListboxControl.setCommandFlags(element, item);

            if (this._collection.has(item.name)) {
                this._element.replaceChild(this._collection[item.name], element);
            }
            else {
                this._element.appendChild(element);
            }

            this._collection.set(item.name, element);
            this._cache.set(item.name, item);
        }

        private onuseraction(event: MouseEvent) {
            let source = event.target as HTMLElement;

            if(source != null && ! source.hasAttribute("command")) {
                source = source.parentElement;
            }

            if (source != null && source.hasAttribute("command")) {

                let attributeFolderName = source
                    .parentElement
                    .parentElement.getAttribute(ListboxControl.ATTR_1);

                let attributeCommand = Number.parseInt(source.getAttribute("command"));
                let raiseEvent: boolean = true;

                if(attributeCommand == Flags.ClientCommands.Delete) {
                    raiseEvent = confirm(this._dialogMessage);
                }
                if(raiseEvent) {
                    this.onaction(
                        attributeFolderName,
                        attributeCommand);
                }
            }
        }

        public change(item: Interfaces.IDownloadRecord) {
            if (this._collection.has(item.name)) {
                this._cache.set(item.name, item);

                let element: HTMLLIElement = this._collection.get(item.name)

                element.setAttribute(ListboxControl.ATTR_2, String(item.state));

                // Das aktuelle Icon beim Löschen behalten.
                // Verhindert ein kurzes Flackern des Icons wenn der Download gelöscht wird.
                if (item.state != Application.Flags.State.Deleting) {
                    element.children[0].textContent = Converter.Icon.Convert(item);
                }

                ListboxControl.setDescription(element, item);
                ListboxControl.setCommandFlags(element, item);
            }
        }

        public remove(name: string) {
            if (this._collection.has(name)) {
                this.removeItemSafe(this._collection.get(name));
                this._collection.delete(name);
                this._cache.delete(name);
            }
        }

        public clear() {
            while (this._element.children.length != 1) {
                this.removeItemSafe(this._element.children[1]);
            }

            this._collection.clear();
            this._cache.clear();
        }

        private removeItemSafe(item: Element) {
            item.removeEventListener("click", this.onuseraction.bind(this));
            this._element.removeChild(item);
        }
        
        private static cloneItem(): HTMLLIElement {

            let template: HTMLTemplateElement = document.getElementById('listitem_template') as HTMLTemplateElement;
            let template_child: HTMLLIElement = template.content.firstElementChild.cloneNode(true) as HTMLLIElement;

            if (template_child == null) {
                throw Error("Failed to create a listboxitem from the template.")
            }

            return template_child;
        }

        private static SchedulerIcon_Start: string = "play_arrow";
        private static SchedulerIcon_Stop: string = "pause";

        public updateSchedulerIcon() {
            let activeDownloads: number = 0;

            this._cache.forEach(download => {
                if (download.state == Application.Flags.State.Queued || 
                    download.state == Application.Flags.State.Running) {
                        activeDownloads++;
                    }
            });

            let isDownloadActive = (activeDownloads == 0);

            // Reduzierung von Neuzeichnen in Browsern.
            if (this._wasDownloadActive != isDownloadActive) {
                this._wasDownloadActive = isDownloadActive;

                if (activeDownloads == 0) {
                    this._scheduler.innerText = ListboxControl.SchedulerIcon_Start;
                } else {
                    this._scheduler.innerText = ListboxControl.SchedulerIcon_Stop;
                }
            }
        }

        public updatePlaceholder() {
            this._placeholder.hidden = (this._collection.size != 0);
        }

        private static setDescription(element: HTMLElement, torrent: Interfaces.IDownloadRecord) {
            switch(torrent.state) {
                case Application.Flags.State.Running: {
                    this.onStatusRunning(element, torrent);
                    break;
                }
                case Application.Flags.State.Failed: {
                    this.onStatusFailed(element, torrent);
                    break;
                }
            }
        }

        private static onStatusRunning(element: HTMLElement, download: Interfaces.IDownloadRecord) {

            let elementDownload     = element.getElementsByClassName("download")[0] as HTMLSpanElement;
            let elementRetry        = element.getElementsByClassName("retry")[0] as HTMLSpanElement;
            let elementCheckStatus  = element.getElementsByClassName("checkStatus")[0] as HTMLSpanElement;

            elementDownload.hidden = true;
            elementRetry.hidden = true;
            elementCheckStatus.hidden = true;

            switch(download.meta) {
                case Application.Flags.Meta.CheckStatus: {
                    elementCheckStatus.hidden = false;
                    break;
                }
                case Application.Flags.Meta.Retry: {
                    elementRetry.hidden = false;
                    break;
                }
                case Application.Flags.Meta.Extract: {
                    (elementDownload.children[1] as HTMLSpanElement).hidden = false;
                }   
                case Application.Flags.Meta.Download: {
                    (elementDownload.children[0] as HTMLSpanElement).hidden = false;
                }
                case Application.Flags.Meta.Extract:         
                case Application.Flags.Meta.Download: {

                    elementDownload.hidden = false;

                    let labelDownload   = (elementDownload.children[0] as HTMLSpanElement);
                    let labelExtract    = (elementDownload.children[1] as HTMLSpanElement);

                    // Not perfect but ok.
                    if(download.meta == Application.Flags.Meta.Download) {
                        labelDownload.hidden = false;
                        labelExtract.hidden = true;
                    }
                    else {
                        labelDownload.hidden = true;
                        labelExtract.hidden = false;
                    }

                    let elementPercent  = elementDownload.getElementsByClassName("infoPercent")[0] as HTMLSpanElement;
                    let elementTime     = elementDownload.getElementsByClassName("infoTime")[0] as HTMLSpanElement;
                    let elementSpeed    = elementDownload.getElementsByClassName("infoSpeed")[0] as HTMLSpanElement;

                    if(download.groupPercent != null)
                    {
                        elementPercent.hidden = false;
                        elementPercent.firstChild.textContent = String(download.groupPercent);
                    } else {
                        elementPercent.hidden = true;
                    }

                    if(download.secondsToComplete != null && download.secondsToComplete != 0)
                    {
                        elementTime.hidden = false;
                        elementTime.firstChild.textContent = String(Math.round(download.secondsToComplete / 60));
                    } else {
                        elementTime.hidden = true;
                    }

                    if(download.bytesPerSecond != null)
                    {
                        elementSpeed.hidden = false;
                        elementSpeed.firstChild.textContent = Application.Common.ByteSizes.formatBytes(download.bytesPerSecond, 2)
                    } else {
                        elementSpeed.hidden = true;
                    }
                }
            }
        }

        private static onStatusFailed(element: HTMLElement, download: Interfaces.IDownloadRecord) {
            let elementException = element.getElementsByClassName("infoError")[0] as HTMLSpanElement;
            let exceptionList = elementException.children;
                    
            let exceptionMessageFound: boolean = false;

            // Attribute mit dem Fehlertyp vergleichen, bei Übereinstimmung das jeweilige Text-Element mit der übersetzten Fehlermeldung anzeigen.
            for (let index = 0; index < exceptionList.length; index++) {
                const element: HTMLSpanElement = exceptionList[index] as HTMLSpanElement;

                // Fehlertyp vergleichen.
                if(element.getAttribute("data-exception") == download.lastException) {
                    element.hidden = false;
                    exceptionMessageFound = true;
                } else {
                    element.hidden = true;
                }
            }

            // Falls keine übersetzte Fehlermeldung gefunden wurde, den Fehlertyp anzeigen.
            if( ! exceptionMessageFound) {
                exceptionList[0].textContent = download.lastException;
                (exceptionList[0] as HTMLSpanElement).hidden = false;
            }
        }

        private static setCommandFlags(element: HTMLElement, torrent: Application.Interfaces.IDownloadRecord) {

            var buttons = element.children[3].children;

            var buttonStart     = buttons[0] as HTMLButtonElement;
            var buttonCancel    = buttons[1] as HTMLButtonElement
            var buttonRetry     = buttons[2] as HTMLButtonElement
            var buttonRemove    = buttons[3] as HTMLButtonElement

            let state = torrent.state;

            // Kann Vorgang hinzufügen.
            buttonStart.hidden  = ! (state == Application.Flags.State.Idle);

            // Kann Vorgang abbrechen.
            buttonCancel.hidden = ! (state == Application.Flags.State.Running ||
                                     state == Application.Flags.State.Queued)

            // Kann Vorgang wiederholen.
            buttonRetry.hidden  = ! (state == Application.Flags.State.Failed ||
                                     state == Application.Flags.State.Completed ||
                                     state == Application.Flags.State.Canceled);

            // Kann Vorgang löschen.
            buttonRemove.hidden = ! (state == Application.Flags.State.Idle ||
                                     state == Application.Flags.State.Failed ||
                                     state == Application.Flags.State.Completed ||
                                     state == Application.Flags.State.Canceled)
        }
    }
}