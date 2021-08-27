using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Singleton;

namespace Sinedo.Hosted
{
    public class FileSystemMonitor : IHostedService
    {
        private readonly FileSystemWatcher fileWatcher;
        private readonly DownloadScheduler scheduler;
        private readonly ILogger<FileSystemMonitor> logger;

        #region Constants

        /// <summary>
        /// Filter für Dateien die überwacht werden sollen.
        /// </summary>
        private const string FileSystemMonitorFilter = "*.txt";

        #endregion

        public FileSystemMonitor(Configuration configuration, DownloadScheduler scheduler, ILogger<FileSystemMonitor> logger)
        {
            this.scheduler = scheduler;
            this.logger = logger;

            // Dateisystemüberwachung einrichten.
            fileWatcher = new FileSystemWatcher(configuration.DownloadDirectory, FileSystemMonitorFilter)
            {
                // Dateinamen und letzten Schreibzugriff überwachen.
                // Letzten Schreibzugriff = Erkennen wenn in einer leeren Datei Links via Notepad eingefügt werden. 
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
            };
            fileWatcher.Created += OnCreated;
            fileWatcher.Changed += OnCreated;
            fileWatcher.IncludeSubdirectories = false;

            configuration.RegisterForUpdates(() => {
                lock (fileWatcher)
                {
                    string path = configuration.DownloadDirectory;

                    if (fileWatcher.Path != path) {
                        fileWatcher.Path = path;
                    }
                }
            });
        }

        /// <summary>
        /// Durchsucht den überwachten Ordner nach Dateien.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Vorhandene Dateien hinzufügen.
            foreach (string filepath in Directory.GetFiles(
                fileWatcher.Path,
                fileWatcher.Filter, SearchOption.TopDirectoryOnly))
            {

                AddToDownloads(filepath, false);
            }
            // Monitor aktivieren.
            fileWatcher.EnableRaisingEvents = true;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Deaktiviert die Dateisystemüberwachung.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Monitor deaktivieren.
            lock (fileWatcher)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Eine gefundene Datei hinzufügen.
        /// </summary>
        /// <param name="filepath">Der vollständige Pfad zur Datei.</param>
        public void AddToDownloads(string filepath, bool autostart)
        {
            try
            {
                string filename = Path.GetFileNameWithoutExtension(filepath);
                string[] files = File.ReadAllLines(filepath);

                if(files.Any()) {
                    scheduler.Create(filename, files, autostart, skipIfContains: true);
                }
            }
            catch
            {
                // Log
            }
        }

        #region Private

        /// <summary>
        /// Wird ausgelöst wenn eine neue Datei in dem überwachten Ordner gefunden wird.
        /// </summary>
        private void OnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            lock(fileWatcher)
            {
                switch(fileSystemEventArgs.ChangeType) {
                    case WatcherChangeTypes.Created:
                    {
                        logger.LogDebug("FileSystemMonitor: '{0}'", fileSystemEventArgs.FullPath);

                        try {
                            FileInfo fileInfo = new (fileSystemEventArgs.FullPath);

                            if (fileInfo.Exists && fileInfo.Length != 0) {
                                AddToDownloads(fileInfo.FullName, true);
                            }
                        } catch (Exception ex) {
                            logger.LogError(ex, "FileSystemMonitor could not add the file '{0}'.", fileSystemEventArgs.Name);

                        }
                        break;
                    }
                }
            }
        }

        #endregion
    }
}