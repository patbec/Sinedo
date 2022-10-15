using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sharehoster.Exceptions;
using Sinedo.Components;
using Sinedo.Components.Logging;
using Sinedo.Exceptions;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        #region Properties

        /// <summary>
        /// Gibt den aktuellen Loginabieter zurück.
        /// </summary>
        private ILogger Logger => _logger;

        /// <summary>
        /// Gibt den aktuellen Anbieter für Benutzereinstellungen zurück.
        /// </summary>
        private IConfiguration Configuration => _configuration;

        #endregion

        /// <summary>
        /// Klasse mit Dependency-Injection erstellen.
        /// </summary>
        public LoginController(IConfiguration configuration, ILogger<LoginController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Gibt die Anmeldeseite zurück.
        /// </summary>
        [Route("login")]
        public IActionResult Index()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if (!Configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/");
            }

            return View();
        }

        /// <summary>
        /// Verarbeitet das eingegebene Anmeldetoken.  
        /// </summary>
        /// <param name="password">Das Kennwort.</param>
        /// <param name="page">Seite an die nach dem Login weitergeleitet wird.</param>
        [Route("login")]
        [HttpPost]
        public async Task<IActionResult> Index(string password, string page)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!Configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            Logger.LogDebug("New authentication request started.");

            try
            {
                if (password == null || !Configuration.PasswordHash.SequenceEqual(Singleton.Configuration.ComputeHash(password)))
                {
                    throw new InvalidPasswordException();
                }

                var claimsIdentity = new ClaimsIdentity("Login");

                var cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                var cookiePrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(cookieScheme,
                                              cookiePrincipal);

                Logger.LogInformation("Authentication was successful.");

                if (page != null)
                {
                    return Redirect("/" + page);
                }

                return Redirect("/");
            }
            catch (InvalidPasswordException ie)
            {
                Logger.LogWarning(ie, "Authentication failed.");
                ModelState.AddModelError("credentials", "The entered password is incorrect.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Authentication for user failed unexpected.");
                ModelState.AddModelError("error", "The login has unexpectedly failed, the error code is: " + ex.HResult);
            }

            // Login fehlgeschlagen.
            return View();
        }

        /// <summary>
        /// Löscht das Anmelde-Cookie.
        /// </summary>
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if (!Configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            await HttpContext.SignOutAsync();
            return Redirect("/login");
        }

        /// <summary>
        /// Seite zum Ändern des Kennwortes.
        /// </summary>
        [Route("change")]
        public IActionResult Change()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if (!Configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/login?page=change"); // ToDo: Check
            }

            return View();
        }

        /// <summary>
        /// Verarbeitet das neu eingegebene Kennwort.
        /// </summary>
        /// <param name="newPassword">Das neue Kennwort.</param>
        [Route("change")]
        [HttpPost]
        public IActionResult Change(string newPassword)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!Configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/login?page=change");
            }

            Logger.LogDebug("New password change request started.");

            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                {
                    throw new InvalidPasswordPolicyException();
                }

                Configuration.PasswordHash = Singleton.Configuration.ComputeHash(newPassword);

                Logger.LogInformation("Password change was successful.");
                return Redirect("/");
            }
            catch (InvalidPasswordPolicyException ae)
            {
                Logger.LogWarning(ae, "Password change for user failed.");
                ModelState.AddModelError("policy", "The entered password is not allowed.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Passwort change for user failed unexpected.");
                ModelState.AddModelError("error", "The password change has unexpectedly failed, the error code is: " + ex.HResult);
            }

            // Kennwort Änderung fehlgeschlagen.
            return View();
        }
    }
}
