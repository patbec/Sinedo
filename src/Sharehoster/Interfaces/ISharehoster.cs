using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Sharehoster.Interfaces
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
        /// Optionale Prüfsumme der Datei.
        /// </summary>
        public string Hash { get; init; }

        /// <summary>
        /// Größe des Datei in Bytes.
        /// </summary>
        public long Size { get; init; }

        /// <summary>
        /// Ursprungslink der Datei.
        /// </summary>
        public Uri Link { get; init; }
    }
    
    /// <summary>
    /// Schnittstelle um Dateien von einem Anbieter herunterzuladen.
    /// </summary>
    public interface ISharehoster
    {
        /// <summary>
        /// Eindeutiger Bezeichner des Anbieters.
        /// </summary>
        Uri Identifier { get; }

        /// <summary>
        /// Konfiguriert den Anbieter und setzt den Token-Cache zurück.
        /// </summary>
        /// <remarks>Hier dürfen keine Fehlermeldungen geworfen werden, dieser Code wird beim Starten des Serverss aufgerufen.</remarks>
        /// <param name="username">Benutzername des Anbieters.</param>
        /// <param name="password">Kennwort oder Token des Anbieters.</param>
        /// <param name="parameter">Optionale Einstellungen für den Anbieters.</param>
        void Configure(string username, string password, string parameter);

        /// <summary>
        /// Gibt einen Wert zurück, ob die angegebene Datei vom Hoster unterstützt wird.
        /// </summary>
        /// <remarks>Hier dürfen keine Fehlermeldungen geworfen werden, sonst wird der Anbieter übersprungen.</remarks>
        /// <param name="fileUri">Adresse zur Datei.</param>
        /// <returns>Gibt an ob die Datei unterstützt wird.</returns>
        bool IsSupported(Uri fileUri);

        /// <summary>
        /// Gibt einen Wert zurück, ob die angegebene Datei vom Hoster unterstützt wird.
        /// </summary>
        /// <remarks>Hier dürfen keine Fehlermeldungen geworfen werden, sonst wird der Anbieter übersprungen.</remarks>
        /// <param name="file">Datei vom Sharehoster.</param>
        /// <returns>Gibt an ob die Datei unterstützt wird.</returns>
        bool IsSupported(SharehosterFile file);

        /// <summary>
        /// Ruft Informationen über die angegebene Datei ab.
        /// </summary>
        /// <param name="fileUri">Adresse zur Datei.</param>
        /// <param name="cancellationToken">Abbruchstoken für den Vorgang.</param>
        /// <returns>Objekt mit Informationen der angegebenen Datei.</returns>
        /// <exception cref="ArgumentNullException">Der angegebene Link ist Null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Der angegebene Link enthält keine Id.</exception>
        /// <exception cref="WebException">Die Api des Sharehosters hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="UnsupportedStatusCodeException">Die Antwort vom Server wird von dieser Erweiterung nicht unterstützt.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        SharehosterFile GetFileInfo(Uri fileUri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ruft den Stream der angegebenen Datei ab.
        /// </summary>
        /// <param name="file">Datei vom Sharehoster.</param>
        /// <param name="startPosition">Download ab einer Byte-Position Fortsetzen.</param>
        /// <param name="cancellationToken">Abbruchstoken für den Vorgang.</param>
        /// <returns>Stream mit dem Inhalt.</returns>
        /// <exception cref="ArgumentNullException">Die angegebene Datei ist ungültig.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Die Startposition liegt außerhalb der gültigen Bereiches.</exception>
        /// <exception cref="BadHttpRequestException">Der Server hat beim Verarbeiten der Anfrage einen Fehlercode zurückgegeben.</exception>
        /// <exception cref="HttpRequestException">Die Anfrage konnte nicht an den Server gesendet werden.</exception>
        /// <exception cref="InvalidFileException">Die Datei ist nicht mehr online oder wurde nicht gefunden.</exception>
        /// <exception cref="InvalidCredentialsException">Der Anmeldetoken ist abgelaufen oder ungültig.</exception>
        /// <exception cref="InvalidResponseException">Die Antwort vom Server konnte nicht gelesen werden.</exception>
        /// <exception cref="TaskCanceledException">Die Anfrage wurde abgebrochen.</exception>
        Stream GetFileStream(SharehosterFile file, long startPosition, CancellationToken cancellationToken = default);
    }
}