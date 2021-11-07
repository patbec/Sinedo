using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Logging;
using Sinedo.Exceptions;
using Sinedo.Models;
using Sinedo.Singleton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sinedo.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ILogger<SettingsController> _logger;
        private readonly Configuration _configuration;
        private readonly DownloadScheduler _scheduler;
        private readonly WebViewLoggerProvider _webViewLoggerProvider;

        #region Properties

        /// <summary>
        /// Gibt den aktuellen Loginabieter zurück.
        /// </summary>
        private ILogger Logger => _logger;

        /// <summary>
        /// Gibt den aktuellen Anbieter für Benutzereinstellungen zurück.
        /// </summary>
        private Configuration Configuration => _configuration;

        #endregion

        /// <summary>
        /// Klasse mit Dependency-Injection erstellen.
        /// </summary>
        public SettingsController(Configuration configuration, WebViewLoggerProvider webViewLoggerProvider, DownloadScheduler scheduler, ILogger<SettingsController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _webViewLoggerProvider = webViewLoggerProvider;
            _scheduler = scheduler;
        }

        #region Settings

        [Route("Settings/Downloads")]
        public IActionResult Downloads()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return Redirect("/Setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            return View(
                Configuration.GetSettings());
        }

        /// <summary>
        /// Verarbeitet das eingegebenen Einstellungen.  
        /// </summary>
        /// <param name="ReturnUrl">Seite an die nach dem Login weitergeleitet wird.</param>
        [Route("Settings/Downloads")]
        [HttpPost]
        public IActionResult Settings(string sharehosterUsername, 
                                      string sharehosterPassword,
                                      uint internetConnectionInMbits,
                                      uint concurrentDownloads,
                                      string downloadDirectory,
                                      string isExtractingEnabled,
                                      string extractingDirectory,
                                      string ReturnUrl)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return Forbid();
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                // Neue Einstellungen prüfen und speichern.
                Configuration.SetGeneralSettings(sharehosterUsername,
                                                 sharehosterPassword,
                                                 internetConnectionInMbits,
                                                 concurrentDownloads,
                                                 downloadDirectory,
                                                 isExtractingEnabled != null,
                                                 extractingDirectory);


                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Settings cloud not be saved.");
                ModelState.AddModelError("error", "Saving the settings failed, the error code is: " + ex.HResult);
            }

            // Einstellungsseite mit eingegebenen Einstellungen und dem Fehler zurückgeben.
            SettingsRecord settings = new ()
            {
                SharehosterUsername = sharehosterUsername,
                SharehosterPassword = sharehosterPassword,
                InternetConnectionInMbits = internetConnectionInMbits,
                DownloadDirectory = downloadDirectory,
                IsExtractingEnabled = isExtractingEnabled != null,
                ExtractingDirectory = extractingDirectory,
            };
            return View(settings);
        }

        [Route("Settings/Server")]
        public IActionResult Server()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return Redirect("/Setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            return View();
        }

        #endregion
    }
}
