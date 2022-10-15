using Microsoft.AspNetCore.Mvc;
using Sinedo.Singleton;

namespace Sinedo.Controllers
{
    public class IndexController : Controller
    {
        private readonly IConfiguration _configuration;

        #region Properties

        /// <summary>
        /// Gibt den aktuellen Anbieter für Benutzereinstellungen zurück.
        /// </summary>
        private IConfiguration Configuration => _configuration;


        #endregion

        /// <summary>
        /// Klasse mit Dependency-Injection erstellen.
        /// </summary>
        public IndexController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gibt die Startseite zurück.
        /// </summary>
        [Route("")]
        public IActionResult Index()
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if (!Configuration.IsSetupCompleted)
            {
                return Redirect("/setup");
            }

            // Prüfen ob sich der Benutzer anmelden muss.
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/login");
            }

            return View();
        }
    }
}
