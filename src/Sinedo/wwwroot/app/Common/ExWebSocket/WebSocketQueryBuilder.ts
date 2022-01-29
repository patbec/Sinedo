/// <reference path="../../Flags/WebSocketChannel.ts" />

namespace Application.Common.ExWebSocket {

    export class WebSocketQueryBuilder {

        private _query: Map<string, string> = new Map<string, string>();

        /**
         * Der Anwendungsname des Clients.
         */
        public set friendlyName(value: string) {
            this._query.set("friendlyName", value);
        }

        /**
         * Channels von denen Nachrichten empfangen werden sollen.
         */
        public set channels(value: Application.Flags.WebSocketChannel[]) {
            this._query.set("channels", value.join(','));
        }

        public buildQuery(): string {
            if (!this._query.has("friendlyName")) {
                throw new Error("FriendlyName attribute must be specified.")
            }

            if (!this._query.has("channels")) {
                throw new Error("Channels attribute must be specified.")
            }

            let queryString: string = "";

            this._query.forEach((value, key) => {
                if (queryString != "") {
                    queryString += "&";
                }
                queryString += `${key}=${encodeURIComponent(value)}`;
            });

            return queryString;
        }
    }
}