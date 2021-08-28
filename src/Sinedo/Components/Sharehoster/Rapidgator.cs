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

namespace Sinedo.Components.Sharehoster
{
    public class Rapidgator : ISharehoster
    {
        private readonly HttpClient client = new();
        private readonly Uri identifier = new("https://rapidgator.net");

        #region  Properties

        /// <summary>
        /// Eindeutiger Bezeichner des Dienstes.
        /// </summary>
        public Uri Identifier
        {
            get => identifier;

        }
        
        #endregion

        public Rapidgator() {
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
        public async Task<SharehosterFile> GetFileInfoAsync(string fileId, string token, CancellationToken cancellationToken)
        {
            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "file_id", fileId },
                { "token", token }
            };

            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/file/info", parameters);
            string requestResult = await client.GetStringAsync(requestUrl ?? "", cancellationToken);

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
        public async Task<string> GetDownloadUrlAsync(string fileId, string token, CancellationToken cancellationToken)
        {
            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "file_id", fileId },
                { "token", token }
            };

            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/file/download", parameters);
            string requestResult = await client.GetStringAsync(requestUrl ?? "", cancellationToken);

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
        public async Task<string> GetAccessToken(string username, string password, CancellationToken cancellationToken)
        {
            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "login", username },
                { "password", password }
            };
            
            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/user/login", parameters);
            string requestResult = await client.GetStringAsync(requestUrl, cancellationToken);

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

                        return token;
                    }
                    case 401: {
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

        public async Task DownloadFileAsync(SharehosterFile file, Stream targetStream, Action<long> report, string token, CancellationToken cancellationToken)
        {
            string rawUrl = await GetDownloadUrlAsync(file.Uid, token, cancellationToken);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rawUrl);
            request.ContinueTimeout = 4000;
            request.Timeout = 4000;
            request.ReadWriteTimeout = 4000;
            request.KeepAlive = false;

            // Start- oder Endposition im Stream angeben.
            if (targetStream.Length != 0)
                request.Headers.Add("Range", $"bytes={targetStream.Length}-{file.Size}");

            using var response = request.GetResponse();
            using var webStream = response.GetResponseStream();

            // Jumbo-Buffer erstellen.
            byte[] buffer = new byte[65536];

            // Anzahl der gelesenen Bytes in einer Sequenz.
            int bytesRead;

            // Kopieren bis keine Bytes mehr gelesen wurden.
            while ((bytesRead = webStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Buffer in die Datei schreiben.
                targetStream.Write(buffer, 0, bytesRead);

                cancellationToken.ThrowIfCancellationRequested();

                // Gelesene Bytes an den Fortschrittsanbieter übermitteln.
                report(bytesRead);
            } 
        }
    }
}