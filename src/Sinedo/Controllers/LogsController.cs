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

        /// <summary>
        /// Klasse mit Dependency-Injection erstellen.
        /// </summary>
        public LogsController(WebViewLoggerProvider webViewLoggerProvider)
        {
            _webViewLoggerProvider = webViewLoggerProvider;
        }

        public class Test {
            public WebViewLogger[] Loggers { get; init; }

            public WebViewLogger SelectedLogger { get; init; }
        }

        /// <summary>
        /// Gibt die Log-Seite zurück.
        /// </summary>
        [Route("Logs")]
        public IActionResult Index(string component)
        {
            Test model;

            if(component != null) {
                var selectedLogger = _webViewLoggerProvider.Loggers.FirstOrDefault(o => o.ComponentName == component);

                if(selectedLogger != null) {
                    model = new Test() {
                        Loggers = _webViewLoggerProvider.Loggers,
                        SelectedLogger = selectedLogger,
                    };

                    return View(model);
                }
            }

            model = new Test() {
                Loggers = _webViewLoggerProvider.Loggers,
            };

            return View(model);
        }

        /// <summary>
        /// Erstellt eine vollständige Sicherung der Logeinträge.
        /// </summary>
        [Route("Logs/CreateBackup")]
        public IActionResult CreateBackup()
        {
            using MemoryStream ms = new();
            using ZipArchive archive = new(ms, ZipArchiveMode.Create, true);

            foreach (WebViewLogger logger in _webViewLoggerProvider.Loggers)
            {
                ZipArchiveEntry archiveEntry = archive.CreateEntry(logger.ComponentNamespace + ".txt", CompressionLevel.Fastest);

                byte[] data = JsonSerializer.SerializeToUtf8Bytes(logger.GetLogItems());

                using Stream zipStream = archiveEntry.Open();
                zipStream.Write(data, 0, data.Length);
                zipStream.Flush();
            }

            return File(ms.ToArray(), "application/zip", "Sinedo Logs - UTC " + DateTime.UtcNow + ".zip");
        }
    }
}
