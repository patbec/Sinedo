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
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sinedo.Controllers
{
    public class LogsController : Controller
    {
        private readonly WebViewLoggerProvider _webViewLoggerProvider;
        private readonly Configuration _configuration;

        /// <summary>
        /// Klasse mit Dependency-Injection erstellen.
        /// </summary>
        public LogsController(WebViewLoggerProvider webViewLoggerProvider, Configuration configuration)
        {
            _webViewLoggerProvider = webViewLoggerProvider;
            _configuration = configuration;
        }

        public class LogModel {
            public IGrouping<string, WebViewLogger>[] Loggers { get; }

            public WebViewLogger SelectedLogger { get; }

            public LogModel(IGrouping<string, WebViewLogger>[] loggers, WebViewLogger selectedLogger) {
                Loggers = loggers;
                SelectedLogger = selectedLogger;
            }
        }

        /// <summary>
        /// Gibt die Log-Seite zurück.
        /// </summary>
        [Route("Logs")]
        public IActionResult Index(string component)
        {
            // Umleiten wenn kein Passwort eingerichtet wurde.
            if( ! _configuration.IsSetupCompleted)
            {
                return Redirect("/Setup");
            }

            // Prüfen ob der Benutzer angemeldet ist.
            if ( ! User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            WebViewLogger selectedLogger = null;
            IGrouping<string, WebViewLogger>[] sortedLoggers;
            
            
            sortedLoggers = _webViewLoggerProvider.Loggers.Where(e => ! e.Internal || e.ComponentName == "Lifetime")
                                                          .GroupBy(o => o.ComponentNamespace.Split('.').Skip(1).First())
                                                          .OrderBy(o => o.Key)
                                                          .ToArray();

            if(component != null) {
                selectedLogger = _webViewLoggerProvider.Loggers.FirstOrDefault(o => o.ComponentName == component);
            }

            return View(
                new LogModel(sortedLoggers, selectedLogger));
        }

        /// <summary>
        /// Erstellt eine vollständige Sicherung der Logeinträge.
        /// </summary>
        [Route("Logs/CreateBackup")]
        public IActionResult CreateBackup()
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

            using MemoryStream ms = new();

            using (ZipArchive archive = new(ms, ZipArchiveMode.Create, true)) {
                foreach (WebViewLogger logger in _webViewLoggerProvider.Loggers)
                {
                    ZipArchiveEntry archiveEntry = archive.CreateEntry(logger.ComponentNamespace + ".txt", CompressionLevel.Fastest);

                    byte[] data = JsonSerializer.SerializeToUtf8Bytes(logger.GetLogItems());

                    using Stream zipStream = archiveEntry.Open();
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Flush();
                }

                ZipArchiveEntry infoEntry = archive.CreateEntry("Info.txt", CompressionLevel.Fastest);

                byte[] infoData = JsonSerializer.SerializeToUtf8Bytes(SystemRecord.GetSystemInfo());

                using Stream zipInfoStream = infoEntry.Open();
                zipInfoStream.Write(infoData, 0, infoData.Length);
                zipInfoStream.Flush();          
            }

            return File(ms.ToArray(), "application/zip", "Sinedo Logs - UTC " + DateTime.UtcNow + ".zip");
        }
    }
}
