using System;
using System.Collections.Generic;
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
    public class DownloadApiController : ControllerBase
    {     
        private readonly DownloadRepository serviceRepository;
        private readonly DownloadScheduler serviceScheduler;
        private readonly Configuration serviceConfiguration;
        private readonly ILogger<DownloadApiController> logger;

        public DownloadApiController(DownloadRepository serviceRepository, DownloadScheduler serviceScheduler, Configuration serviceConfiguration, ILogger<DownloadApiController> logger)
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
            if(HttpContext.User.Identity.IsAuthenticated && serviceConfiguration.IsSetupCompleted)
            {
                return Ok(SystemRecord.GetSystemInfo());
            }
            
            return Unauthorized();
        }

        [Route("api/scheduler")]
        [Produces("application/json")]
        public ActionResult<DownloadRecord[]> SchedulerInfo()
        {
            if(HttpContext.User.Identity.IsAuthenticated && serviceConfiguration.IsSetupCompleted)
            {
                int downloadsCount = 0;
                int downloadsRunning = 0;
                
                serviceRepository.EnterReadLock(() =>
                {
                    downloadsCount = serviceRepository.AsEnumerable().Count();
                    downloadsRunning = serviceRepository.AsEnumerable().Count(t =>
                    {
                        return t.State == Flags.GroupState.Running ||
                               t.State == Flags.GroupState.Queued ||
                               t.State == Flags.GroupState.Stopping;
                    });
                });
                
                SchedulerInfoRecord info = new() {
                    DownloadsCount = downloadsCount,
                    DownloadsRunning = downloadsRunning
                };

                return Ok(info);
            }
            
            return Unauthorized();
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
        public ActionResult<DownloadRecord[]> GetDownloads([FromQuery] GroupState? filter)
        {
            if(HttpContext.User.Identity.IsAuthenticated && serviceConfiguration.IsSetupCompleted)
            {
                DownloadRecord[] result = null;

                serviceRepository.EnterReadLock(() =>
                {
                    if(filter != null) {
                        result = serviceRepository.AsEnumerable()
                                                  .Where(t => t.State == filter.Value)
                                                  .ToArray();
                    }
                    else {
                        result = serviceRepository.AsEnumerable()
                                                  .ToArray();
                    }
                });

                return Ok(result);
            }
            
            return Unauthorized();
        }

        [Route("api/downloads/{name}")]
        [Produces("application/json")]
        public ActionResult<DownloadRecord> GetDownload(string name)
        {
            if(HttpContext.User.Identity.IsAuthenticated && serviceConfiguration.IsSetupCompleted)
            {
                if(name == null) {
                    return BadRequest();
                }

                DownloadRecord download = null;

                serviceRepository.EnterReadLock(() =>
                {
                    download = serviceRepository.FindOrDefault(name);
                });

                if(download == null) {
                    return NotFound();
                }

                return Ok(download);
            }

            return Unauthorized();
        }

        [Route("downloads/{name}")]
        [HttpPost]
        public ActionResult<DownloadRecord> PostDownload([FromRoute] string name, [FromQuery] string[] files, [FromQuery] string password, [FromQuery] bool autostart = true)
        {
            if(HttpContext.User.Identity.IsAuthenticated && serviceConfiguration.IsSetupCompleted)
            {
                try
                {
                    string createdDownload = null;
                    serviceRepository.EnterWriteLock(() =>
                    {
                        createdDownload = serviceScheduler.Create(name, files, autostart, password);
                    });
                    return CreatedAtAction(nameof(PostDownload), new { name = createdDownload });
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "The download could not be created.");

                    return base.BadRequest(new { error = ex.Message.GetType() });
                }
            }
            
            return Unauthorized();
        }
    }
}
