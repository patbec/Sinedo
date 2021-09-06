/// <reference path="../Common/Control.ts" />
/// <reference path="../Interfaces/ICommand.ts" />

namespace Application.Services {

    export class HyperlinkControl {

        private _elementRoot: HTMLDivElement;
        private _elementRootShow: HTMLDivElement;
        private _elementRootEdit: HTMLDivElement;

        private _elementLinkBox: HTMLUListElement;
        private _elementLinkEdit: HTMLUListElement;

        private _buttonEdit: HTMLButtonElement;
        private _buttonSave: HTMLButtonElement;
        private _buttonCancel: HTMLButtonElement;

        private static MAX_LINKS: number = 3;

        public onaction: ((this: HyperlinkControl, links: Application.Interfaces.ICommandHyperlink[]) => any) | null;

        public constructor() {
            this._elementRoot = Common.Control.get("links");

            this._elementRootShow = this._elementRoot.children[1] as HTMLDivElement;
            this._elementRootEdit = this._elementRoot.children[2] as HTMLDivElement;

            this._elementLinkBox = Common.Control.get("linkbox");
            this._elementLinkEdit = Common.Control.get("linkedit");

            this._buttonEdit    = Common.Control.get("buttonLinksEdit");
            this._buttonSave    = Common.Control.get("buttonLinksSave");
            this._buttonCancel  = Common.Control.get("buttonLinksCancel");


            this._buttonEdit.addEventListener("click", ((e: any) => {
                this.updateEditMode();
                this.editMode = true;
            }).bind(this));

            this._buttonCancel.addEventListener("click", ((e: any) => {
                this.clearEditMode();
                this.editMode = false;
            }).bind(this));

            this._buttonSave.addEventListener("click", ((e: any) => {
                this.sendNewLinks();
                this.editMode = false;
            }).bind(this));
        }

        private set editMode(value: boolean) {
            this._elementRootShow.hidden = value;
            this._elementRootEdit.hidden = ! value;
        }

        /**
         * Setzt die angegebenen Links.
         * Während das Steuerelement bearbeitet wird, werden keine neuen Links angezeigt.
         * @value 
         */
        public updateLinks(links: Application.Interfaces.ICommandHyperlink[]) {
            let linkBox = this._elementLinkBox;

            for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                let listElement = linkBox.children[index] as HTMLLIElement;
                let linkElement = listElement.firstElementChild as HTMLLinkElement;

                let textPrimary = linkElement.firstElementChild as HTMLDivElement;
                let textSecondary = linkElement.lastElementChild as HTMLDivElement;
                    
                if(links[index] != null) {
                    listElement.hidden = false;

                    // Link zuweisen.
                    linkElement.href = links[index].url; 

                    textPrimary.textContent = links[index].displayName;
                    textSecondary.textContent = links[index].url;
                } else {
                    listElement.hidden = true;

                    textPrimary.textContent = "";
                    textSecondary.textContent = "";

                    linkElement.removeAttribute("href");
                }           
            }
        }

        /**
         * Aktualisiert die Eingabesteuerelemente.
         * @value 
         */
         private updateEditMode() {
            let linkBox = this._elementLinkBox;
            let linkEdit = this._elementLinkEdit;

            for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                let listElement = linkBox.children[index] as HTMLLIElement;
                let linkElement = listElement.firstElementChild as HTMLLinkElement;

                let editDiv = linkEdit.children[index].firstElementChild as HTMLDivElement;

                let editFieldDisplayName = editDiv.firstElementChild as HTMLInputElement;
                let editFieldUrl         = editDiv.lastElementChild as HTMLInputElement;

                if(listElement.hidden == false) {
                    editFieldDisplayName.value = linkElement.firstElementChild.textContent;
                    editFieldUrl.value = linkElement.href;
                } else {
                    editFieldDisplayName.value = "";
                    editFieldUrl.value = "";
                }
            }
        }

        /**
         * Bereinigt die Eingabesteuerelemente.
         * @value 
         */
         private clearEditMode() {
            let linkEdit = this._elementLinkEdit;

            for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                let editDiv = linkEdit.children[index].firstElementChild as HTMLDivElement;

                let editFieldDisplayName = editDiv.firstElementChild as HTMLInputElement;
                let editFieldUrl         = editDiv.lastElementChild as HTMLInputElement;

                editFieldDisplayName.value = "";
                editFieldUrl.value = "";
            }
        }

        /**
         * Sendet die Eingaben an den Server.
         * @value 
         */
         private sendNewLinks() {
            let links: Application.Interfaces.ICommandHyperlink[] = [ null, null, null ];

            let linkEdit = this._elementLinkEdit;

            for (let index = 0; index < HyperlinkControl.MAX_LINKS; index++) {
                let editDiv = linkEdit.children[index].firstElementChild as HTMLDivElement;

                let editFieldDisplayName = editDiv.firstElementChild as HTMLInputElement;
                let editFieldUrl         = editDiv.lastElementChild as HTMLInputElement;

                links[index] =
                {
                    displayName: editFieldDisplayName.value,
                    url: editFieldUrl.value,
                };
            }  

            this.onaction(links);
        }
    }
}