using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Sinedo.Flags;
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
        private readonly ILogger<WebSocketRouting> _logger;

        /// <summary>
        /// Erstellt eine neue Middleware um WebSocket-Verbindung für eine Echtzeitübertragung anzunehmen.
        /// </summary>
        public WebSocketRouting(RequestDelegate next, WebSocketRouter commandRouter, ILogger<WebSocketRouting> logger)
        {
            _next = next;
            _commandRouter = commandRouter;
            _logger = logger;
        }

        /// <summary>
        /// Behandelt eingehende Echtzeitverbindungen.
        /// </summary>
        public async Task Invoke(HttpContext httpContext)
        {
            // Prüfen ob eine WebSocket-Verbindung aufgebaut werden soll.
            if (httpContext.Request.Path == "/api/server-connection.ws")
            {
                // Anfrage im Backend verarbeiten.
                await ProcessWebSocketConnectionAsync(httpContext);
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
        private async Task ProcessWebSocketConnectionAsync(HttpContext httpContext)
        {
            try
            {
                HttpStatusCode httpCheckCode = CheckAndParseHeader(httpContext, out WebSocketChannel[] webSocketChannels, out string webSocketFriendlyName);

                if (httpCheckCode != HttpStatusCode.OK)
                {
                    httpContext.Response.StatusCode = (int)httpCheckCode;
                    return;
                }

                var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();

                _logger.LogInformation("Client with IP {ipAddress} has connected to the service.", httpContext.Connection.RemoteIpAddress);

                // Client an das Backend weitergeben.
                await _commandRouter.AttachAsync(
                    webSocket,
                    webSocketChannels,
                    webSocketFriendlyName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The request from client with IP {ipAddress} could not be processed due to an internal error. (500 - InternalServerError)", httpContext.Connection.RemoteIpAddress);

                // Fehlercode zurückgeben: 500 - Interner Server Fehler.
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        private HttpStatusCode CheckAndParseHeader(HttpContext httpContext, out WebSocketChannel[] webSocketChannels, out string webSocketFriendlyName)
        {
            webSocketChannels = null;
            webSocketFriendlyName = null;

            if (!httpContext.User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Client with IP {ipAddress} has sent a request without a valid login. (401 - Unauthorized)", httpContext.Connection.RemoteIpAddress);

                // Fehlercode zurückgeben: 401 - Nicht angemeldet.
                return HttpStatusCode.Unauthorized;
            }

            // if (!httpContext.Request.Query.ContainsKey("version"))
            // {
            //     _logger.LogWarning("Client with IP {ipAddress} has sent a request without the required version parameter. (400 - BadRequest)", httpContext.Connection.RemoteIpAddress);

            //     // Fehlercode zurückgeben: 400 - Fehlender Parameter in der Url.
            //     return HttpStatusCode.BadRequest;
            // }

            // if (httpContext.Request.Query["version"] != AVAILABLE_API_VERSION)
            // {
            //     _logger.LogWarning("Client with IP {ipAddress} has requested Api version {requestedApiVersion}. This server supports only Api version {supportedApiVersion}. (501 - NotImplemented)", httpContext.Connection.RemoteIpAddress, httpContext.Request.Query["version"], AVAILABLE_API_VERSION);

            //     // Fehlercode zurückgeben: 501 - Die angefragte Api-Version wird von diesem Server nicht unterstützt.
            //     return HttpStatusCode.NotImplemented;
            // }

            if (!httpContext.Request.Query.ContainsKey("channels"))
            {
                _logger.LogWarning("Client with IP {ipAddress} has sent a request without the required parameter channels. (400 - BadRequest)", httpContext.Connection.RemoteIpAddress);

                // Fehlercode zurückgeben: 400 - Fehlender Parameter in der Url.
                return HttpStatusCode.BadRequest;
            }

            if (!httpContext.Request.Query.ContainsKey("friendlyName"))
            {
                _logger.LogWarning("Client with IP {ipAddress} has sent a request without the required parameter friendlyName. (400 - BadRequest)", httpContext.Connection.RemoteIpAddress);

                // Fehlercode zurückgeben: 400 - Fehlender Parameter in der Url.
                return HttpStatusCode.BadRequest;
            }

            if (string.IsNullOrWhiteSpace(httpContext.Request.Query["friendlyName"]))
            {
                _logger.LogWarning("Client with IP {ipAddress} has sent a request with empty query parameter friendlyName. (400 - BadRequest)", httpContext.Connection.RemoteIpAddress);

                // Fehlercode zurückgeben: 400 - Ungültiger Wert in einem Url-Parameter.
                return HttpStatusCode.BadRequest;
            }

            // if (!(httpContext.Request.Headers.ContainsKey("Accept") && httpContext.Request.Headers["Accept"] == "application/octet-stream"))
            // {
            //     _logger.LogWarning("Client with IP {0} has sent a request without the required Accept header value application/octet-stream. (406 - NotAcceptable)", httpContext.Connection.RemoteIpAddress);

            //     // Fehlercode zurückgeben: 406 - Nicht unterstützter Accept Header.
            //     return HttpStatusCode.NotAcceptable;
            // }

            if (!httpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Client with IP {ipAddress} must request with a WebSocket connection. (505 - HttpVersionNotSupported)", httpContext.Connection.RemoteIpAddress);

                // Fehlercode zurückgeben: 505 - Keine WebSocket (HTTP2) Verbindung.
                return HttpStatusCode.HttpVersionNotSupported;
            }

            webSocketChannels = GetChannels(httpContext.Request.Query["channels"]);

            if (webSocketChannels == null)
            {
                _logger.LogWarning("Client with IP {ipAddress} has requested a channel that is not supported. The following channels are available: {supportedChannels} (501 - NotImplemented)", httpContext.Connection.RemoteIpAddress, string.Join(", ", Enum.GetNames(typeof(WebSocketChannel))));

                // Fehlercode zurückgeben: 501 - Nicht unterstützte(r) Channel in der Url.
                return HttpStatusCode.NotImplemented;
            }

            webSocketFriendlyName = httpContext.Request.Query["friendlyName"];

            return HttpStatusCode.OK;
        }

        private static WebSocketChannel[] GetChannels(string queryValue)
        {
            string[] stringChannels = queryValue.Split(",");

            if (stringChannels.Length == 0)
            {
                return null;
            }

            WebSocketChannel[] channels = new WebSocketChannel[stringChannels.Length];

            for (int i = 0; i < stringChannels.Length; i++)
            {
                bool isSuccessfully = Enum.TryParse(stringChannels[i], out WebSocketChannel channel);

                if (!isSuccessfully)
                {
                    return null;
                }

                channels[i] = channel;
            }

            return channels;
        }
    }
}
