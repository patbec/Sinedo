using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Sharehoster.Exceptions;
using Sharehoster.Interfaces;

namespace Sharehoster
{
    public class Rapidgator : ISharehoster
    {
        private readonly HttpClient client = new();
        private readonly Uri identifier = new("https://rapidgator.net");

        private string username, password;
        private string currentApiToken;

        #region  Properties

        /// <summary>
        /// Eindeutiger Bezeichner des Dienstes.
        /// </summary>
        public Uri Identifier
        {
            get => identifier;

        }

        #endregion

        public Rapidgator()
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        }

        #region ISharehoster Interface

        /// <summary>
        /// Konfiguriert den Anbieter und setzt den Token-Cache zurück.
        /// </summary>
        /// <remarks>Hier dürfen keine Fehlermeldungen geworfen werden, dieser Code wird beim Starten des Serverss aufgerufen.</remarks>
        /// <param name="username">Benutzername des Anbieters.</param>
        /// <param name="password">Kennwort oder Token des Anbieters.</param>
        /// <param name="parameter">Optionale Einstellungen für den Anbieters.</param>
        public void Configure(string username, string password, string parameter)
        {
            lock (this)
            {
                currentApiToken = null;

                this.username = username;
                this.password = password;
            }
        }

        /// <summary>
        /// Gibt einen Wert zurück, ob die angegebene Datei vom Hoster unterstützt wird.
        /// </summary>
        /// <remarks>Hier dürfen keine Fehlermeldungen geworfen werden, sonst wird der Anbieter übersprungen.</remarks>
        /// <param name="fileUri">Adresse zur Datei.</param>
        /// <returns>Gibt an ob die Datei unterstützt wird.</returns>
        public bool IsSupported(Uri fileUri)
        {
            if (fileUri == null || !fileUri.IsAbsoluteUri)
            {
                return false;
            }

            return fileUri.Host == identifier.Host;
        }

        /// <summary>
        /// Gibt einen Wert zurück, ob die angegebene Datei vom Hoster unterstützt wird.
        /// </summary>
        /// <remarks>Hier dürfen keine Fehlermeldungen geworfen werden, sonst wird der Anbieter übersprungen.</remarks>
        /// <param name="file">Datei vom Sharehoster.</param>
        /// <returns>Gibt an ob die Datei unterstützt wird.</returns>
        public bool IsSupported(SharehosterFile file)
        {
            if (file == null || file.Link == null || !file.Link.IsAbsoluteUri)
            {
                return false;
            }

            return file.Link.Host == identifier.Host;
        }

        /// <summary>
        /// Ruft Informationen über die angegebene Datei ab.
        /// </summary>
        /// <param name="fileUri">Adresse zur Datei.</param>
        /// <param name="cancellationToken">Abbruchstoken für den Vorgang.</param>
        /// <returns>Objekt mit Informationen der angegebenen Datei.</returns>
        /// <exception cref="ArgumentNullException">Der angegebene Link ist Null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Der angegebene Link enthält keine Id.</exception>
        /// <exception cref="BadHttpRequestException">Die Api des Sharehosters hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="UnsupportedStatusCodeException">Die Antwort vom Server wird von dieser Erweiterung nicht unterstützt.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        public SharehosterFile GetFileInfo(Uri fileUri, CancellationToken cancellationToken = default)
        {
            if (fileUri == null)
            {
                throw new ArgumentNullException(nameof(fileUri));
            }
            if (fileUri.Segments.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(fileUri));
            }

            RequestAccessToken(cancellationToken);

            string fileId = fileUri.Segments.Last();

            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "file_id", fileId },
                { "token", currentApiToken }
            };

            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/file/info", parameters);
            string requestResult = client.GetStringAsync(requestUrl, cancellationToken).Result;

            try
            {
                var document = JsonSerializer.Deserialize<JsonElement>(requestResult);

                var statusCode = document.GetProperty("status").GetInt32();
                var statusDetails = document.GetProperty("details").GetString();

                switch (statusCode)
                {
                    case 200:
                        {
                            var node = document.GetProperty("response").GetProperty("file");
                            var name = node.GetProperty("name").GetString();
                            var hash = node.GetProperty("hash").GetString();
                            var size = node.GetProperty("size").GetInt64();

                            return new SharehosterFile()
                            {
                                Uid = fileId,
                                Name = name,
                                Hash = hash,
                                Size = size,
                                Link = fileUri
                            };
                        }
                    case 401:
                        {
                            ResetAccessToken();
                            throw new InvalidCredentialsException(this);
                        }
                    case 404:
                        {
                            throw new InvalidFileException(this, requestUrl);
                        }
                    default:
                        {
                            throw new UnsupportedStatusCodeException(requestUrl, statusCode, statusDetails);
                        }
                }
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is JsonException)
            {
                throw new InvalidResponseException(this, ex);
            }
        }

        /// <summary>
        /// Ruft den Stream der angegebenen Datei ab.
        /// </summary>
        /// <param name="file">Datei vom Sharehoster.</param>
        /// <param name="startPosition">Download ab einer Byte-Position Fortsetzen.</param>
        /// <param name="cancellationToken">Abbruchstoken für den Vorgang.</param>
        /// <returns>Stream mit dem Inhalt.</returns>
        /// <exception cref="ArgumentNullException">Die angegebene Datei ist ungültig.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Die Startposition liegt außerhalb der gültigen Bereiches.</exception>
        /// <exception cref="WebException">Der Server hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        public Stream GetFileStream(SharehosterFile file, long startPosition, CancellationToken cancellationToken = default)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Uid))
            {
                throw new ArgumentNullException(nameof(file));
            }

            string downloadUrl = RequestDownloadUrl(file, cancellationToken);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadUrl);
            request.ContinueTimeout = 4000;
            request.Timeout = 4000;
            request.ReadWriteTimeout = 4000;
            request.KeepAlive = false;

            // Start- oder Endposition im Stream angeben.
            if (startPosition != 0)
            {
                if (startPosition <= file.Size || startPosition > 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(startPosition));
                }

                request.Headers.Add("Range", $"bytes {startPosition}-{file.Size}");
            }

            return request.GetResponse().GetResponseStream();
        }

        #endregion

        #region Internal

        /// <summary>
        /// Ruft Informationen über die angegebene Datei ab.
        /// </summary>
        /// <param name="file">Datei vom Sharehoster.</param>
        /// <param name="cancellationToken">Abbruchstoken für den Vorgang.</param>
        /// <returns>Link zum normalen Herunterladen.</returns>
        /// <exception cref="ArgumentNullException">Die angegebene Datei ist ungültig.</exception>
        /// <exception cref="UnsupportedStatusCodeException">Der vom Server zurückgegebene Statuscode wird nicht unterstützt.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="ExceededTrafficException">Das Downloadlimit wurde beim Hoster überschritten.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        /// <exception cref="AccountExpiredException">Der Zugang zum Account ist abgelaufen.</exception>
        internal string RequestDownloadUrl(SharehosterFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Uid))
            {
                throw new ArgumentNullException(nameof(file));
            }

            RequestAccessToken(cancellationToken);

            // Parameter für die Anfrage. 
            var parameters = new Dictionary<string, string>
            {
                { "file_id", file.Uid },
                { "token", currentApiToken }
            };

            // Request erstellen und Ergebnis auslesen.
            string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/file/download", parameters);
            string requestResult = client.GetStringAsync(requestUrl, cancellationToken).Result;

            try
            {
                var document = JsonSerializer.Deserialize<JsonElement>(requestResult);

                var statusCode = document.GetProperty("status").GetInt32();
                var statusDetails = document.GetProperty("details").GetString();

                switch (statusCode)
                {
                    case 200:
                        {
                            var node = document.GetProperty("response");

                            var downloadUrl = node.GetProperty("download_url").GetString();

                            return downloadUrl;
                        }
                    case 401:
                        {
                            ResetAccessToken();
                            throw new InvalidCredentialsException(this);
                        }
                    case 404:
                        {
                            throw new InvalidFileException(this, requestUrl);
                        }
                    case 423:
                        {
                            throw new ExceededTrafficException(this);
                        }
                    default:
                        {
                            throw new UnsupportedStatusCodeException(requestUrl, statusCode, statusDetails);
                        }
                }
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is JsonException)
            {
                throw new InvalidResponseException(this, ex);
            }
        }

        /// <summary>
        /// Ruft den Anmeldetoken ab und speichert diesen für weitere Anfragen zwischen.
        /// </summary>
        /// <param name="cancellationToken">Abbruchstoken</param>
        /// <exception cref="UnsupportedStatusCodeException">Der vom Server zurückgegebene Statuscode wird nicht unterstützt.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        /// <exception cref="AccountExpiredException">Der Zugang zum Account ist abgelaufen.</exception>
        internal void RequestAccessToken(CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                if (currentApiToken != null)
                {
                    return;
                }

                // Parameter für die Anfrage. 
                var parameters = new Dictionary<string, string>
                {
                    { "login", username },
                    { "password", password }
                };

                // Request erstellen und Ergebnis auslesen.
                string requestUrl = QueryHelpers.AddQueryString("https://rapidgator.net/api/v2/user/login", parameters);
                string requestResult = client.GetStringAsync(requestUrl, cancellationToken).Result;

                try
                {
                    var document = JsonSerializer.Deserialize<JsonElement>(requestResult);

                    var statusCode = document.GetProperty("status").GetInt32();
                    var statusDetails = document.GetProperty("details").GetString();

                    switch (statusCode)
                    {
                        case 200:
                            {
                                var node = document.GetProperty("response");
                                var token = node.GetProperty("token").GetString();
                                var isPremium = node.GetProperty("user").GetProperty("is_premium").GetBoolean();

                                if (!isPremium)
                                {
                                    throw new AccountExpiredException(this);
                                }

                                currentApiToken = token;
                                break;
                            }
                        case 401:
                            {
                                currentApiToken = null;
                                throw new InvalidCredentialsException(this);
                            }
                        default:
                            {
                                throw new UnsupportedStatusCodeException(requestUrl, statusCode, statusDetails);
                            }
                    }
                }
                catch (Exception ex) when (ex is KeyNotFoundException || ex is JsonException)
                {
                    throw new InvalidResponseException(this, ex);
                }
            }
        }

        /// <summary>
        /// Setzt den Anmeldetoken zurück.
        /// </summary>
        internal void ResetAccessToken()
        {
            lock (this)
            {
                currentApiToken = null;
            }
        }

        #endregion
    }
}