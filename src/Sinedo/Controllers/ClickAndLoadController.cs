using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Controllers
{
    /// <summary>
    /// Click&Load Erweiterung
    /// </summary>
    [ApiController]
    public class ClickAndLoadController : ControllerBase
    {
        private readonly ILogger<ClickAndLoadController> logger;
        private readonly DownloadScheduler scheduler;
        private readonly BroadcastQueue broadcaster;


        public ClickAndLoadController(ILogger<ClickAndLoadController> logger, DownloadScheduler scheduler, BroadcastQueue broadcaster)
        {
            this.logger = logger;
            this.scheduler = scheduler;
            this.broadcaster = broadcaster;
        }

        #region Api

        [Route("/addcrypted")]
        [HttpPost]
        public ActionResult AddCrypted()
        {
            // 501	- Not Implemented
            return StatusCode(501);
        }

        [Route("/flash")]
        public IActionResult Index()
        {
            return Content(nameof(Sinedo));
        }

        [Route("/flash/add")]
        [HttpPost]
        public async Task<ActionResult> Add([FromForm] string passwords,
                                            [FromForm] string source,
                                            [FromForm] string package,
                                            [FromForm] string[] urls)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(package))
                {
                    // Alternativen Namen festlegen wenn kein Titel angegeben wurde.
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        package = "Click&Load";
                    }
                    else
                    {
                        package = source;
                    }
                }

                // Dateien hinzufügen.
                await scheduler.CreateAsync(
                    package,
                    urls,
                    passwords,
                    autostart: false);

                logger.LogInformation("{linksNumber} Links were added successfully.", urls.Length);
                return Ok();
            }
            catch (Exception ex)
            {
                ex = new ClickAndLoadException(ex);

                SendException(ex);
                return BadRequest(new { error = ex.Message.GetType() });
            }
        }

        [Route("/flash/addcrypted2")]
        [HttpPost]
        public async Task<ActionResult> AddCrypted2([FromForm] string passwords,
                                                    [FromForm] string source,
                                                    [FromForm] string package,
                                                    [FromForm] string jk,
                                                    [FromForm] string crypted)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(package))
                {
                    // Alternativen Namen festlegen wenn kein Titel angegeben wurde.
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        package = "Click&Load";
                    }
                    else
                    {
                        package = source;
                    }
                }

                // Inhalt entschlüsseln.
                var container = Container.Decrypt(
                    package,
                    passwords,
                    source,
                    jk,
                    crypted);

                // Dateien hinzufügen.
                await scheduler.CreateAsync(
                    container.Name,
                    container.Urls,
                    null,
                    autostart: false);

                logger.LogInformation("{linksNumber} Links were added successfully.", container.Urls.Length);
                return Ok();
            }
            catch (Exception ex)
            {
                ex = new ClickAndLoadException(ex);

                SendException(ex);
                return BadRequest(new { error = ex.Message.GetType() });
            }
        }

        [Route("/flash/checkSupportForUrl")]
        [HttpPost]
        public ActionResult CheckSupportForUrl()
        {
            // 501	- Not Implemented
            return StatusCode(501);
        }

        #endregion

        /// <summary>
        /// Zeigt die angegebene Fehlermeldung allen Clients an.
        /// </summary>
        private void SendException(Exception exception)
        {
            logger.LogError(exception, "Links could not be added.");

            NotificationRecord clientNotification = NotificationRecord.FromException(exception);

            broadcaster.Add(Flags.CommandFromServer.Notification, clientNotification);
        }
    }
}