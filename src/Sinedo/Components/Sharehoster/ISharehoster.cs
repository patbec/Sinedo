using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Sinedo.Components.Sharehoster
{
    /// <summary>
    /// Informationen über eine Datei bei einem Anbieter.
    /// </summary>
    public record SharehosterFile
    {
        /// <summary>
        /// Eindeutiger Bezeichner der Datei.
        /// </summary>
        public string Uid { get; init; }

        /// <summary>
        /// Ungeprüfter Dateiname.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Prüfsumme der Datei.
        /// </summary>
        public string Hash { get; init; }

        /// <summary>
        /// Größe des Datei.
        /// </summary>
        public long Size { get; init; }
    }
    
    /// <summary>
    /// Schnittstelle um Dateien von einem Anbieter herunterzuladen.
    /// </summary>
    public interface ISharehoster
    {
        /// <summary>
        /// Eindeutiger Bezeichner des Dienstes.
        /// </summary>
        Uri Identifier { get; }

        /// <summary>
        /// Ruft Informationen über die angegebene Datei ab.
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
        SharehosterFile GetInfos(string fileId, CancellationToken cancellationToken);

        /// <summary>
        /// Ruft den Link zum Herunterladen ab.
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
        string GetDownloadUrl(string fileId, CancellationToken cancellationToken);

        // /// <summary>
        // /// Ruft den Anmeldetoken ab.
        // /// </summary>
        // /// <param name="username">Benutzername</param>
        // /// <param name="password">Passwort</param>
        // /// <param name="cancellationToken">Abbruchstoken</param>
        // /// <returns>Das abgerufene Zugriffstoken.</returns>
        // /// <exception cref="BadHttpRequestException">Der Server hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        // /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        // /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        // /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        // /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        // string GetToken(CancellationToken cancellationToken);

        Stream GetFile(SharehosterFile file, long startPosition, CancellationToken cancellationToken);
    }
}