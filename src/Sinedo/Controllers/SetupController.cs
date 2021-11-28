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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sinedo.Controllers
{
    public class SetupController : Controller
    {
        private readonly ILogger<SetupController> _logger;
        private readonly Configuration _configuration;

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
        public SetupController(Configuration configuration, ILogger<SetupController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Gibt die Einrichtungsseite zurück.
        /// </summary>
        [Route("setup")]
        public IActionResult Index()
        {
            // Umleiten wenn die Einrichtung bereits abgeschlossen wurde.
            if(Configuration.IsSetupCompleted)
            {
                return Redirect("/");
            }

            return View();
        }

        /// <summary>
        /// Verarbeitet das eingegebene Anmeldetoken.  
        /// </summary>
        /// <param name="newPassword">Das beim Setup angegebene Kennwort.</param>
        [Route("setup")]
        [HttpPost]
        public async Task<IActionResult> Index(string newPassword)
        {
            // Abbrechen falls ein Passwort eingerichtet wurde.
            if(Configuration.IsSetupCompleted)
            {
                return Redirect("/");
            }

            Logger.LogDebug("New setup request started.");

            try
            {
                if(string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4) {
                    throw new InvalidPasswordPolicyException();
                }
                // Save the new password.
                Configuration.PasswordHash = Configuration.ComputeHash(newPassword);

                ClaimsIdentity claimsIdentity = new("Login");

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                Logger.LogInformation("Setup was successful.");
                return Redirect("/settings");
            }
            catch (InvalidPasswordPolicyException ae)
            {
                Logger.LogWarning(ae, "Setup failed.");
                ModelState.AddModelError("policy", "The entered password is not allowed.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Setup for user failed unexpected.");
                ModelState.AddModelError("error", "The setup has unexpectedly failed, the error code is: " + ex.HResult);
            }

            // Login fehlgeschlagen.
            return View();  
        }
    }
}
