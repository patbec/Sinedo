using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Exceptions;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public class Configuration
    {
        private readonly ILogger<Configuration> logger;
        private readonly Serializer configurationFile;
        private readonly List<Action> callbacks = new ();

        #region Properties

        /// <summary>
        /// Gibt an ob eine Einrichtung abgeschlossen wurde.
        /// </summary>
        public bool IsSetupCompleted { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] PasswordHash { get; set; }
        /// <summary>
        /// Benutzername für den Dienst.
        /// </summary>
        public string SharehosterUsername { get; private set; }
        /// <summary>
        /// Kennwort für den Dienst.
        /// </summary>
        public string SharehosterPassword { get; private set; }
        /// <summary>
        /// Geschwindigkeit der Internetverbindung in Mbits.
        /// </summary>
        public uint InternetConnectionInMbits { get; private set; }
        /// <summary>
        /// Anzahl der gleichzeitigen Downloads.
        /// </summary>
        public uint ConcurrentDownloads { get; private set; }
        /// <summary>
        /// Zielordner für die heruntergeladenen Dateien.
        /// </summary>
        public string DownloadDirectory { get; private set; }
        /// <summary>
        /// Gibt an ob heruntergeladene Dateien entpackt werden sollen.
        /// </summary>
        public bool IsExtractingEnabled { get; private set; }
        /// <summary>
        /// Zielordner für die entpackten Dateien.
        /// </summary>
        public string ExtractingDirectory { get; private set; }

        #endregion

        #region Constructor

        public Configuration(ILogger<Configuration> logger)
        {
            this.logger = logger;

            string configFile = Path.Combine(AppDirectories.ConfigDirectory, "config.json");

            configurationFile = new Serializer(configFile);

            try
            {
                SettingsFile configurationData = configurationFile.Load<SettingsFile>();

                // Setup ist abgeschlossen, wenn ein Passwort gesetzt wurde.
                IsSetupCompleted = configurationData.PasswordHash != null &&
                                   configurationData.PasswordHash.Length != 0;


                //
                // Prüfung auf fehlerhafte Einstellungen
                //

                if(configurationData.InternetConnectionInMbits == 0) {
                    throw new ArgumentOutOfRangeException(nameof(InternetConnectionInMbits), configurationData.InternetConnectionInMbits, "The value must not be 0.");
                }
                if(configurationData.ConcurrentDownloads == 0 || configurationData.ConcurrentDownloads > 20) {
                    throw new ArgumentOutOfRangeException(nameof(ConcurrentDownloads), configurationData.ConcurrentDownloads, "The value must be within 1 to 20.");
                }

                //
                // Aktuelle Einstellungen übernehmen
                //

                PasswordHash                = configurationData.PasswordHash;
                SharehosterUsername         = configurationData.SharehosterUsername;
                SharehosterPassword         = configurationData.SharehosterPassword;
                InternetConnectionInMbits   = configurationData.InternetConnectionInMbits;
                ConcurrentDownloads         = configurationData.ConcurrentDownloads;
                DownloadDirectory           = configurationData.DownloadDirectory;
                IsExtractingEnabled         = configurationData.IsExtractingEnabled;
                ExtractingDirectory         = configurationData.ExtractingDirectory;

                logger.LogInformation("Settings file loaded successfully.");
            }
            catch(Exception ex)
            {
                if(ex is FileNotFoundException) {
                    logger.LogInformation("A new settings file will be created.");
                }
                else {
                    logger.LogCritical(ex, "The settings could not be loaded due to an error, the setup mode is used.");
                }

                //
                // Bei einem Fehler Standardwerte verwenden.
                //

                PasswordHash = null;
                SharehosterUsername = "";
                SharehosterPassword = "";
                InternetConnectionInMbits = 50;
                ConcurrentDownloads = 2;
                DownloadDirectory = Path.Combine(AppDirectories.HomeDirectory, "Downloads");
                ExtractingDirectory = Path.Combine(AppDirectories.HomeDirectory, "Sinedo");
                IsExtractingEnabled = false;
            }
        }
        
        #endregion

        /// <summary>
        /// Meldet geänderte Einstellungen an die angegebene Aktion.
        /// </summary>
        public void RegisterForUpdates(Action callback) {
            lock(this)
            {
                callbacks.Add(callback);
            }
        }

        /// <summary>
        /// Speichert die aktuellen Einstellungen.
        /// </summary>
        private void Save() {
            SettingsFile configurationData = new ()
            {
                PasswordHash                = PasswordHash,
                SharehosterUsername         = SharehosterUsername,
                SharehosterPassword         = SharehosterPassword,
                InternetConnectionInMbits   = InternetConnectionInMbits,
                ConcurrentDownloads         = ConcurrentDownloads,
                DownloadDirectory           = DownloadDirectory,
                IsExtractingEnabled         = IsExtractingEnabled,
                ExtractingDirectory         = ExtractingDirectory,
            };

            configurationFile.Save(configurationData);
        }

        /// <summary>
        /// Setzt und speichert ein neues Passwort zum Anmelden.
        /// </summary>
        /// <param name="password">Passwort im Klartext.</param>
        public void SetLoginPassword(string password)
        {
            lock(this)
            {
                if(string.IsNullOrWhiteSpace(password) || password.Length < 4) {
                    throw new InvalidPasswordPolicyException();
                }

                PasswordHash = ComputeHash(password);
                Save();

                IsSetupCompleted = true;

                logger.LogInformation("The login password has been updated.");
            }
        }

        /// <summary>
        /// Prüft ob das angegebene Passwort übereinstimmt.
        /// </summary>
        /// <param name="password">Passwort im Klartext.</param>
        public bool CheckLoginPassword(string password)
        {
            lock(this)
            {
                if(string.IsNullOrWhiteSpace(password)) {
                    return false;
                }

                return ComputeHash(password).SequenceEqual(PasswordHash);
            }
        }

        /// <summary>
        /// Prüft und speichert die allgemeinen Einstellungen.
        /// </summary>
        /// <param name="sharehosterUsername">Benutzername für den Dienst.</param>
        /// <param name="sharehosterPassword">Kennwort für den Dienst.</param>
        /// <param name="internetConnectionInMbits">Geschwindigkeit der Internetverbindung in Mbits.</param>
        /// <param name="concurrentDownloads">Anzahl der gleichzeitigen Downloads.</param>
        /// <param name="downloadDirectory">Zielordner für die heruntergeladenen Dateien.</param>
        /// <param name="isExtractingEnabled">Gibt an ob heruntergeladene Dateien entpackt werden sollen.</param>
        /// <param name="extractingDirectory">Zielordner für die entpackten Dateien.</param>
        [Obsolete]
        public void SetGeneralSettings(string sharehosterUsername, string sharehosterPassword, uint internetConnectionInMbits, uint concurrentDownloads, string downloadDirectory, bool isExtractingEnabled, string extractingDirectory)
        {
            lock(this)
            {
                //
                // Prüfung auf fehlerhafte Einstellungen
                //

                if(internetConnectionInMbits == 0) {
                    throw new ArgumentOutOfRangeException(nameof(InternetConnectionInMbits), internetConnectionInMbits, "The value must not be 0.");
                }
                if(concurrentDownloads == 0 || concurrentDownloads > 20) {
                    throw new ArgumentOutOfRangeException(nameof(ConcurrentDownloads), concurrentDownloads, "The value must be within 1 to 20.");
                }

                //
                // Der hinterlegte Password-Hash zum Anmelden wird unverändert übernommen.
                //

                // Neue Einstellungen übernehmen.
                SharehosterUsername         = sharehosterUsername;
                SharehosterPassword         = sharehosterPassword;
                InternetConnectionInMbits   = internetConnectionInMbits;
                ConcurrentDownloads         = concurrentDownloads;
                DownloadDirectory           = downloadDirectory;
                IsExtractingEnabled         = isExtractingEnabled;
                ExtractingDirectory         = extractingDirectory;
                
                Save();

                logger.LogInformation("The general settings have been updated.");

                foreach (var callback in callbacks) {
                    callback();
                }

                logger.LogInformation("Callbacks for settings have been completed.");
            }
        }

        /// <summary>
        /// Prüft und speichert die angegebene Einstellung.
        /// </summary>
        /// <param name="settingName">Name der Einstellung.</param>
        /// <param name="settingValue">Wert der Einstellung.</param>
        public void SetGeneralSetting(string settingName, string settingValue)
        {
            if(settingValue == null) {
                throw new ArgumentNullException(nameof(settingValue));
            }

            lock(this)
            { 
                //
                // Prüfung auf fehlerhafte Einstellungen
                //

                switch(settingName)
                {
                    case "internetConnectionInMbits": {
                        uint internetConnectionInMbits = uint.Parse(settingValue);

                        if(internetConnectionInMbits == 0) {
                            throw new ArgumentOutOfRangeException(nameof(InternetConnectionInMbits), internetConnectionInMbits, "The value must not be 0.");
                        }

                        InternetConnectionInMbits = internetConnectionInMbits;
                        break;
                    }
                    case "concurrentDownloads": {
                        uint concurrentDownloads = uint.Parse(settingValue);

                        if(concurrentDownloads == 0 || concurrentDownloads > 20) {
                            throw new ArgumentOutOfRangeException(nameof(ConcurrentDownloads), concurrentDownloads, "The value must be within 1 to 20.");
                        }

                        InternetConnectionInMbits = concurrentDownloads;
                        break;
                    }
                    case "downloadDirectory": {
                        string downloadDirectory = settingValue;

                        DownloadDirectory = downloadDirectory;
                        break;
                    }
                    case "isExtractingEnabled": {
                        bool isExtractingEnabled = bool.Parse(settingValue);

                        IsExtractingEnabled = isExtractingEnabled;
                        break;
                    }
                    case "extractingDirectory": {
                        string extractingDirectory = settingValue;

                        ExtractingDirectory = extractingDirectory;
                        break;
                    }
                    default: {
                        throw new ArgumentException("The setting named '{0}' is not supported.", settingName);
                    }
                }

                Save();

                logger.LogInformation("The general settings have been updated.");

                foreach (var callback in callbacks) {
                    callback();
                }

                logger.LogInformation("Callbacks for settings have been completed.");
            }
        }

        [Obsolete]
        public SettingsRecord GetSettings()
        {
            lock(this)
            {
                // Erstellt eine neue Struktur ohne geschützte Daten.
                return new SettingsRecord()
                {
                    SharehosterUsername         = this.SharehosterUsername,
                    SharehosterPassword         = this.SharehosterPassword,
                    InternetConnectionInMbits   = this.InternetConnectionInMbits,
                    ConcurrentDownloads         = this.ConcurrentDownloads,
                    DownloadDirectory           = this.DownloadDirectory,
                    ExtractingDirectory         = this.ExtractingDirectory,
                    IsExtractingEnabled         = this.IsExtractingEnabled,
                };
            }
        }
        
        private static byte[] ComputeHash(string passwordToHash) {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(passwordToHash);

            HashAlgorithm sha = SHA512.Create();
            return sha.ComputeHash(passwordBytes);
        }
    }
}