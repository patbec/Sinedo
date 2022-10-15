using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    /// http://0.0.0.0:2222/api/system
    /// http://0.0.0.0:2222/api/scheduler
    /// http://0.0.0.0:2222/api/downloads
    /// http://0.0.0.0:2222/api/downloads?filter=Idle
    /// http://0.0.0.0:2222/api/downloads?filter=Queued
    /// http://0.0.0.0:2222/api/downloads?filter=Canceled
    /// http://0.0.0.0:2222/api/downloads?filter=Failed
    /// http://0.0.0.0:2222/api/downloads?filter=Running
    /// http://0.0.0.0:2222/api/downloads?filter=Completed
    /// http://0.0.0.0:2222/api/downloads?filter=Deleting
    /// http://0.0.0.0:2222/api/downloads?filter=Stopping
    /// http://0.0.0.0:2222/api/downloads?filter=Unsupported
    /// http://0.0.0.0:2222/api/downloads/{name}
    /// 
    /// RestApi POST:
    /// http://0.0.0.0:2222/api/downloads
    /// </summary>
    [ApiController]
    public class RestApiController : ControllerBase
    {
        private readonly DownloadRepository serviceRepository;
        private readonly DownloadScheduler serviceScheduler;
        private readonly Configuration serviceConfiguration;
        private readonly ILogger<RestApiController> logger;

        public RestApiController(DownloadRepository serviceRepository, DownloadScheduler serviceScheduler, Configuration serviceConfiguration, ILogger<RestApiController> logger)
        {
            this.serviceRepository = serviceRepository;
            this.serviceScheduler = serviceScheduler;
            this.serviceConfiguration = serviceConfiguration;
            this.logger = logger;
        }

        [Route("api/system")]
        [Produces("application/json")]
        public ActionResult<DownloadRecord[]> SystemInfo()
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!serviceConfiguration.IsSetupCompleted)
            {
                return Forbid();
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            return Ok(SystemRecord.GetSystemInfo());
        }

        [Route("api/scheduler")]
        [Produces("application/json")]
        public async Task<ActionResult<DownloadRecord[]>> SchedulerInfo()
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!serviceConfiguration.IsSetupCompleted)
            {
                return Forbid();
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }


            int downloadsCount = 0;
            int downloadsRunning = 0;

            using (await serviceRepository.Context.ReaderLockAsync())
            {
                downloadsCount = serviceRepository.AsEnumerable().Count();
                downloadsRunning = serviceRepository.AsEnumerable().Count(t =>
                {
                    return t.State == DownloadState.Running ||
                           t.State == DownloadState.Queued ||
                           t.State == DownloadState.Stopping;
                });
            };

            SchedulerInfoRecord info = new()
            {
                DownloadsCount = downloadsCount,
                DownloadsRunning = downloadsRunning
            };

            return Ok(info);
        }

        /// <summary>
        /// Sample:
        /// http://0.0.0.0:5000/api/downloads
        /// http://0.0.0.0:5000/api/downloads?filter=Queued
        /// http://0.0.0.0:5000/api/downloads?filter=Running
        /// </summary>
        /// <param name="filter">Optional: Filtert die Liste.</param>
        [Route("api/downloads")]
        [Produces("application/json")]
        public async Task<ActionResult<DownloadRecord[]>> GetDownloads([FromQuery] DownloadState? filter)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!serviceConfiguration.IsSetupCompleted)
            {
                return Forbid();
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            DownloadRecord[] result = null;

            using (await serviceRepository.Context.ReaderLockAsync())
            {
                if (filter != null)
                {
                    result = serviceRepository.AsEnumerable()
                                              .Where(t => t.State == filter.Value)
                                              .ToArray();
                }
                else
                {
                    result = serviceRepository.AsEnumerable()
                                              .ToArray();
                }
            };

            return Ok(result);
        }

        [Route("api/downloads/{name}")]
        [Produces("application/json")]
        public async Task<ActionResult<DownloadRecord>> GetDownload(string name)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!serviceConfiguration.IsSetupCompleted)
            {
                return Forbid();
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            if (name == null)
            {
                return BadRequest();
            }

            DownloadRecord download = null;

            using (await serviceRepository.Context.ReaderLockAsync())
            {
                download = serviceRepository.FindOrDefault(name);
            };

            if (download == null)
            {
                return NotFound();
            }

            return Ok(download);
        }

        [Route("api/downloads/{name}")]
        [HttpPost]
        public async Task<ActionResult<DownloadRecord>> PostDownload([FromRoute] string name, [FromQuery] string[] files, [FromQuery] string password, [FromQuery] bool autostart = true)
        {
            // Status-Code 403 zurückgeben, wenn die Einrichtung nicht abgeschlossen wurde.
            if (!serviceConfiguration.IsSetupCompleted)
            {
                return Forbid();
            }

            // Status-Code 401 zurückgeben, wenn Benutzer nicht angemeldet ist.
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            try
            {
                string createdDownload = await serviceScheduler.CreateAsync(name, files, password, autostart);

                return CreatedAtAction(nameof(PostDownload), new { name = createdDownload });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The download could not be created.");

                return base.BadRequest(new { error = ex.Message.GetType() });
            }
        }
    }
}
