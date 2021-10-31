using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Sinedo.Singleton;

namespace Sinedo.Middleware
{
    /// <summary>
    /// Middleware um WebSocket-Verbindung für eine Echtzeitübertragung anzunehmen.
    /// </summary>
    public class WebSocketRouting
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketRouter _commandRouter;

        /// <summary>
        /// Erstellt eine neue Middleware um WebSocket-Verbindung für eine Echtzeitübertragung anzunehmen.
        /// </summary>
        public WebSocketRouting(RequestDelegate next, WebSocketRouter commandRouter)
        {
            _next = next;
            _commandRouter = commandRouter;
        }

        /// <summary>
        /// Behandelt eingehende Echtzeitverbindungen.
        /// </summary>
        public async Task Invoke(HttpContext httpContext)
        {
            // Prüfen ob eine WebSocket-Verbindung aufgebraucht werden soll.
            if (httpContext.Request.Path == "/api/server-connection.ws")
            {
                // Prüfen ob der Nutzer mit einem gültigen Cookie angemeldet ist.
                if(httpContext.User.Identity.IsAuthenticated)
                {
                    // Prüfen ob die angefragte API-Version unterstützt wird.
                    if(httpContext.Request.Query["version"] == "1")
                    {
                        // Anfrage im Backend verarbeiten.
                        await ProcessWebSocketConnection(httpContext);
                    }
                    else
                    {
                        // Fehlercode zurückgeben.
                        httpContext.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                    }
                }
                else {
                    // Fehlercode zurückgeben.
                    httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
            }
            else
            {
                // In der Pipeline weitergeben.
                await _next(httpContext);
            }
        }

        /// <summary>
        /// Verarbeitet die eingehende WebSocket-Verbindung.
        /// </summary>
        private async Task ProcessWebSocketConnection(HttpContext httpContext)
        {
            // Prüfen ob die Verbindung HTTP2 nutzt.
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();

                // Handle um die Verbindung offen zu halten.
                var webSocketTcs = new TaskCompletionSource<object>();

                // Client an das Backend weitergeben.
                _commandRouter.Attach(
                    webSocket, 
                    webSocketTcs);

                // Warten bis die Verbindung durch den Client geschlossen wird.
                await webSocketTcs.Task;
            }
            else
            {
                // Fehlercode zurückgeben.
                httpContext.Response.StatusCode = (int)HttpStatusCode.HttpVersionNotSupported;
            }
        }
    }
}
