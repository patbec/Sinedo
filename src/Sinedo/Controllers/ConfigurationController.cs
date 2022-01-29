using Microsoft.AspNetCore.Mvc;
using Sinedo.Singleton;
using System;

namespace Sinedo.Controllers
{
    public class ConfigurationController : Controller
    {
        private readonly Configuration _configuration;

        /// <summary>
        /// Klasse mit Dependency-Injection erstellen.
        /// </summary>
        public ConfigurationController(Configuration configuration)
        {
            this._configuration = configuration;
        }

        [Route("configuration")]
        public IActionResult RedirectToFirstSite()
        {
            return Redirect("/configuration/downloads");
        }

        [Route("configuration/downloads")]
        public IActionResult Downloads()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! _configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Redirect("/login?page=configuration");
            }

            return View(_configuration);
        }

        [Route("configuration/accounts")]
        public IActionResult Account()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! _configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Redirect("/login?page=configuration");
            }

            return View(_configuration);
        }

        [Route("configuration/server")]
        public IActionResult Server()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! _configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Redirect("/login?page=configuration");
            }

            return View(_configuration);
        }

        #region RestApi

        [Route("api/configuration")]
        [Produces("application/json")]
        [HttpGet]
        public IActionResult Configuration()
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if( ! _configuration.IsSetupCompleted)
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
                return Ok(_configuration);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("api/configuration")]
        [Produces("application/json")]
        [HttpGet]
        public IActionResult ConfigurationGet(string name)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if( ! _configuration.IsSetupCompleted)
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
                if (name == null) {
                    return Ok(_configuration);
                }

                object value = name switch
                {
                    "ipaddress" => _configuration.IPAddress,
                    "port" =>_configuration.Port,
                    "internetconnectioninmbits" => _configuration.InternetConnectionInMbits,
                    "concurrentdownloads" => _configuration.ConcurrentDownloads,
                    "downloaddirectory" => _configuration.DownloadDirectory,
                    "isextractingenabled" => _configuration.IsExtractingEnabled,
                    "extractingdirectory" => _configuration.ExtractingDirectory,
                    _ => null
                };

                if(value == null) {
                    return NotFound($"The setting '{name}' was not found.");
                }
                return Ok(value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("api/configuration")]
        [HttpPost]
        public IActionResult ConfigurationPost(string name, [FromBody] string value)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if( ! _configuration.IsSetupCompleted)
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
                if(string.IsNullOrWhiteSpace(name)) {
                    throw new ArgumentNullException(nameof(name));
                }

                switch(name.ToLower())
                {
                    case "ipaddress": {
                        _configuration.IPAddress = value;
                        break;
                    }
                    case "port": {
                        _configuration.Port = int.Parse(value);
                        break;
                    }          
                    case "internetconnectioninmbits": {
                        _configuration.InternetConnectionInMbits = uint.Parse(value);
                        break;
                    }
                    case "concurrentdownloads": {
                        _configuration.ConcurrentDownloads = uint.Parse(value);
                        break;
                    }
                    case "downloaddirectory": {
                        _configuration.DownloadDirectory = value;
                        break;
                    }
                    case "isextractingenabled": {
                        _configuration.IsExtractingEnabled = bool.Parse(value);
                        break;
                    }
                    case "extractingdirectory": {
                        _configuration.ExtractingDirectory = value;
                        break;
                    }
                    default: {
                        return NotFound($"The setting '{name}' was not found.");
                    }
                }

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
