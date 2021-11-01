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
    public class IndexController : Controller
    {
        private readonly ILogger<IndexController> _logger;
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
        public IndexController(Configuration configuration, WebViewLoggerProvider webViewLoggerProvider, DownloadScheduler scheduler, ILogger<IndexController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _webViewLoggerProvider = webViewLoggerProvider;
            _scheduler = scheduler;
        }

        #region Main Site

        /// <summary>
        /// Gibt die Willkommensseite zurück.
        /// </summary>
        [Route("")]
        public IActionResult Index()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return RedirectToAction(nameof(Setup));
            }

            // Prüfen ob sich der Benutzer anmelden muss.
            if ( ! User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        #endregion

        #region Setup

        /// <summary>
        /// Gibt die Einrichtungsseite zurück.
        /// </summary>
        [Route("Setup")]
        public IActionResult Setup()
        {
            // Umleiten wenn die Einrichtung bereits abgeschlossen wurde.
            if(Configuration.IsSetupCompleted)
            {
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        /// <summary>
        /// Verarbeitet das eingegebene Anmeldetoken.  
        /// </summary>
        /// <param name="newPassword">Das beim Setup angegebene Kennwort.</param>
        /// <param name="ReturnUrl">Seite an die nach dem Setup weitergeleitet wird.</param>
        [Route("Setup")]
        [HttpPost]
        public async Task<IActionResult> Setup(string newPassword, string ReturnUrl)
        {
            // Abbrechen falls ein Passwort eingerichtet wurde.
            if(Configuration.IsSetupCompleted)
            {
                return Forbid();
            }

            Logger.LogDebug("New setup request started.");

            try
            {
                var claimsIdentity = new ClaimsIdentity("Login");

                // Save the new password.
                Configuration.SetLoginPassword(newPassword);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                Logger.LogInformation("Setup was successful.");
                return RedirectToAction(nameof(Settings));
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

        #endregion

        #region Login and Logout

        /// <summary>
        /// Gibt die Anmeldeseite zurück.
        /// </summary>
        [Route("Login")]
        public IActionResult Login()
        {       
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return RedirectToAction(nameof(Setup));
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        /// <summary>
        /// Verarbeitet das eingegebene Anmeldetoken.  
        /// </summary>
        /// <param name="password">Das Kennwort.</param>
        /// <param name="ReturnUrl">Seite an die nach dem Login weitergeleitet wird.</param>
        [Route("Login")]
        [HttpPost]
        public async Task<IActionResult> Login(string password, string ReturnUrl)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return Forbid();
            }

            Logger.LogDebug("New authentication request started.");

            try
            {
                var claimsIdentity = new ClaimsIdentity("Login");

                if( ! Configuration.CheckLoginPassword(password)) {
                    throw new InvalidCredentialsException();
                }

                var cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                var cookiePrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(cookieScheme,
                                              cookiePrincipal);

                Logger.LogInformation("Authentication was successful.");
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidCredentialsException ie)
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
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return RedirectToAction(nameof(Setup));
            }

            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Change Password

        /// <summary>
        /// Seite zum Ändern des Kennwortes.
        /// </summary>
        [Route("Change")]
        public IActionResult Change()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return RedirectToAction(nameof(Setup));
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        /// <summary>
        /// Verarbeitet das neu eingegebene Kennwort.  
        /// </summary>
        /// <param name="newPassword">Das neue Kennwort.</param>
        /// <param name="ReturnUrl">Seite an die nach dem Login weitergeleitet wird.</param>
        [Route("Change")]
        [HttpPost]
        public IActionResult Change(string newPassword, string ReturnUrl)
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

            Logger.LogDebug("New password change request started.");

            try
            {
                 Configuration.SetLoginPassword(newPassword);

                Logger.LogInformation("Password change was successful.");
                return RedirectToAction(nameof(Index));
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

        #endregion

        #region Settings

        /// <summary>
        /// Seite für Einstellungen.
        /// </summary>
        [Route("Settings")]
        public IActionResult Settings()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! Configuration.IsSetupCompleted)
            {
                return RedirectToAction(nameof(Setup));
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Login));
            }

            return View(
                Configuration.GetSettings());
        }

        /// <summary>
        /// Verarbeitet das eingegebenen Einstellungen.  
        /// </summary>
        /// <param name="ReturnUrl">Seite an die nach dem Login weitergeleitet wird.</param>
        [Route("Settings")]
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

        #endregion
    }
}
