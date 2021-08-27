using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Controllers
{
    /// <summary>
    /// RestApi GET:
    /// http://0.0.0.0:2222/api/debug
    /// </summary>
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly Configuration configuration;

        public DebugController(Configuration configuration) {
            this.configuration = configuration;
        }

        [Route("api/debug")]
        [Produces("application/json")]
        public ActionResult<DownloadRecord[]> PrintPaths()
        {
            if(HttpContext.User.Identity.IsAuthenticated)
            {
                try {
                    Dictionary<string, string> paths = new ();

                    string appName = nameof(Sinedo).ToLower();


                    string applicationConfig    = Path.Combine(AppDirectories.ConfigDirectory, "sinedo", "config.json");
                    string applicationHome      = AppDirectories.HomeDirectory;
                    string applicationData      = Directory.GetCurrentDirectory();

                    paths.Add("Application Config", applicationConfig);
                    paths.Add("Application Home", applicationHome);
                    paths.Add("Application Data", applicationData);

                    paths.Add("Download Directory", configuration.DownloadDirectory);
                    paths.Add("Extracting Directory", configuration.ExtractingDirectory);

                    return Ok(paths);
                } catch (Exception ex) {
                    return Problem(ex.Message);
                }
            }
            
            return Unauthorized();
        }
    }
}