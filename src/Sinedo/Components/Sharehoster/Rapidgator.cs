using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Sinedo.Exceptions;
using Sinedo.Components;
using System.Net;
using Sinedo.Components.Common;
using System.Security.Principal;

namespace Sinedo.Components.Sharehoster
{
    public class Rapidgator : ISharehoster
    {
        private readonly HttpClient client = new();
        private readonly Uri identifier = new("https://rapidgator.net");
        private readonly NetworkCredential credentials;

        private static string currentApiToken;

        #region  Properties

        /// <summary>
        /// Eindeutiger Bezeichner des Dienstes.
        /// </summary>
        public Uri Identifier
        {
            get => identifier;

        }
        
        #endregion

        public Rapidgator(NetworkCredential credentials)
        {
            if(string.IsNullOrEmpty(credentials.UserName) || string.IsNullOrEmpty(credentials.Password)) {
                throw new MissingCredentialsException();
            }

            this.credentials = credentials;

            client.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Ruft den Link zum Herunterladen ab.
        /// </summary>
        /// <param name="fileId">Bezeichner der Datei</param>
        /// <param name="token">Anmeldetoken</param>
        /// <param name="cancellationToken">Abbruchstoken</param>
        /// <returns>Objekt mit den Dateiinfos des Anbieters.</returns>
        /// <exception cref="BadHttpRequestException">Der Server hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        public SharehosterFile GetInfos(string fileId, CancellationToken cancellationToken)
        {
            string token = GetToken(cancellationToken);

            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "file_id", fileId },
                { "token", token }
            };

            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/file/info", parameters);
            string requestResult = client.GetStringAsync(requestUrl ?? "", cancellationToken).Result;

            try
            {
                var document = JsonSerializer.Deserialize<JsonElement>(requestResult);

                var statusCode = document.GetProperty("status").GetInt32();
                var statusDetails = document.GetProperty("details").GetString();

                switch(statusCode) {
                    case 200: {
                        var node = document.GetProperty("response").GetProperty("file");
                        var name = node.GetProperty("name").GetString();
                        var hash = node.GetProperty("hash").GetString();
                        var size = node.GetProperty("size").GetInt64();

                        return new SharehosterFile() {
                            Uid = fileId,
                            Name = name,
                            Hash = hash,
                            Size = size
                        };
                    }
                    case 401: {
                        currentApiToken = null;
                        throw new InvalidCredentialsException();
                    }
                    case 404: {
                        throw new InvalidFileException(fileId);
                    }
                    default: {
                        throw new BadHttpRequestException(statusDetails);
                    }
                }
            }
            catch (Exception ex) when (ex is KeyNotFoundException|| ex is JsonException)
            {
                throw new InvalidResponseException(ex);
            }
        }

        /// <summary>
        /// Ruft Informationen über die angegebene Datei ab.
        /// </summary>
        /// <param name="fileId">Bezeichner der Datei</param>
        /// <param name="token">Anmeldetoken</param>
        /// <param name="cancellationToken">Abbruchstoken</param>
        /// <returns>Link zum normalen Herunterladen.</returns>
        /// <exception cref="BadHttpRequestException">Der Server hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        public string GetDownloadUrl(string fileId, CancellationToken cancellationToken)
        {
            string token = GetToken(cancellationToken);

            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "file_id", fileId },
                { "token", token }
            };

            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/file/download", parameters);
            string requestResult = client.GetStringAsync(requestUrl ?? "", cancellationToken).Result;

            try
            {
                var document = JsonSerializer.Deserialize<JsonElement>(requestResult);

                var statusCode = document.GetProperty("status").GetInt32();
                var statusDetails = document.GetProperty("details").GetString();

                switch(statusCode) {
                    case 200: {
                        var node = document.GetProperty("response");

                        var downloadUrl = node.GetProperty("download_url").GetString();

                        return downloadUrl;
                    }
                    case 401: {
                        currentApiToken = null;
                        throw new InvalidCredentialsException();
                    }
                    case 404: {
                        throw new InvalidFileException(fileId);
                    }
                    default: {
                        throw new BadHttpRequestException(statusDetails);
                    }
                }
            }
            catch (Exception ex) when (ex is KeyNotFoundException|| ex is JsonException)
            {
                throw new InvalidResponseException(ex);
            }
        }

        /// <summary>
        /// Ruft den Anmeldetoken ab.
        /// </summary>
        /// <param name="username">Benutzername</param>
        /// <param name="password">Passwort</param>
        /// <param name="cancellationToken">Abbruchstoken</param>
        /// <returns>Das abgerufene Zugriffstoken.</returns>
        /// <exception cref="BadHttpRequestException">Der Server hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        public string GetToken(CancellationToken cancellationToken)
        {
            lock(this)
            {
                if(currentApiToken != null) {
                    return currentApiToken;
                }

                // Parameter für die Anfrage. 
                var parameters = new Dictionary<string, string>
                {
                    { "login", credentials.UserName },
                    { "password", credentials.Password }
                };
                
                // Request erstellen und Ergebnis auslesen.
                string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/user/login", parameters);
                string requestResult = client.GetStringAsync(requestUrl, cancellationToken).Result;

                try
                {
                    var document = JsonSerializer.Deserialize<JsonElement>(requestResult);

                    var statusCode = document.GetProperty("status").GetInt32();
                    var statusDetails = document.GetProperty("details").GetString();

                    switch(statusCode) {
                        case 200: {
                            var node = document.GetProperty("response");
                            var token = node.GetProperty("token").GetString();
                            var isPremium = node.GetProperty("user").GetProperty("is_premium").GetBoolean();

                            if( ! isPremium) {
                                throw new AccountExpiredException();
                            }

                            currentApiToken = token;
                            return token;
                        }
                        case 401: {
                            currentApiToken = null;
                            throw new InvalidCredentialsException();
                        }
                        default: {
                            throw new BadHttpRequestException(statusDetails);
                        }
                    }
                }
                catch (Exception ex) when (ex is KeyNotFoundException|| ex is JsonException)
                {
                    throw new InvalidResponseException(ex);
                }   
            }
        }

        public Stream GetFile(SharehosterFile file, long startPosition, CancellationToken cancellationToken)
        {
            string rawUrl = GetDownloadUrl(file.Uid, cancellationToken);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rawUrl);
            request.ContinueTimeout = 4000;
            request.Timeout = 4000;
            request.ReadWriteTimeout = 4000;
            request.KeepAlive = false;

            // Start- oder Endposition im Stream angeben.
            if (startPosition != 0)
                request.Headers.Add("Range", $"bytes {startPosition}-{file.Size}");

            return request.GetResponse().GetResponseStream();
        }
    }
}