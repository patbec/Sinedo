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


        [Route("settings/server")]
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

        #region Api

        /// <summary>
        /// 
        /// </summary>
        [Route("api/settings")]
        [HttpPost]
        public IActionResult Settings(string name, [FromBody] string value)
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
                Configuration.SetGeneralSetting(name, value);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion
    }
}
