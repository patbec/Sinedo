using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Background
{
    public class StorageService : IHostedService
    {
        private readonly List<ushort> _list;
        private readonly BroadcastQueue _queue;
        private readonly SetupBuilder _setupBuilder;
        private readonly DownloadScheduler _scheduler;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageService> _logger;

        private DriveInfo drive;
        private StorageMonitor monitor;
        private FileSystemWatcher fileWatcher;

        #region Constants

        /// <summary>
        /// Filter für Dateien die überwacht werden sollen.
        /// </summary>
        private const string FileSystemMonitorFilter = "*.json";

        #endregion

        public StorageService(BroadcastQueue queue, SetupBuilder setupBuilder, DownloadScheduler scheduler, IConfiguration configuration, ILogger<StorageService> logger)
        {
            _list = new();

            _setupBuilder = setupBuilder;
            _queue = queue;
            _scheduler = scheduler;
            _configuration = configuration;
            _logger = logger;

            monitor = new(configuration.DownloadDirectory);
            monitor.StorageOnline += StorageOnline;
            monitor.StorageUpdate += StorageUpdate;
            monitor.StorageOffline += StorageOffline;

            configuration.PropertyChanged += (s, p) =>
            {
                lock (this)
                {
                    _logger.LogInformation("Settings changed, new path is set.");

                    monitor.Stop();
                    monitor.StorageOnline -= StorageOnline;
                    monitor.StorageUpdate -= StorageUpdate;
                    monitor.StorageOffline -= StorageOffline;

                    monitor = new(configuration.DownloadDirectory);
                    monitor.StorageOnline += StorageOnline;
                    monitor.StorageUpdate += StorageUpdate;
                    monitor.StorageOffline += StorageOffline;
                    monitor.Start();
                }
            };
        }

        #region Events

        /// <summary>
        /// Wird aufgerufen wenn der Datenträger verfügbar ist.
        /// </summary>
        private void StorageOnline()
        {
            try
            {
                lock (this)
                {
                    _logger.LogInformation("The path '{downloadDirectory}' is available.", _configuration.DownloadDirectory);

                    // Datenträgerüberwachung einrichten.
                    CreateDiskSpaceWatcher(_configuration.DownloadDirectory);

                    // Dateisystemüberwachung einrichten.
                    CreateFileSystemWatcher(_configuration.DownloadDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The specified path '{downloadDirectory}' cannot be monitored for changes.", _configuration.DownloadDirectory);
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn der Datenträger aktualisiert werden soll.
        /// </summary>
        private void StorageUpdate()
        {
            try
            {
                lock (this)
                {
                    // Im Speicherplatz-Verlauf einen neuen Wert hinzufügen und an verbundene Clients senden.
                    UpdateDiskSpace();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Calculation of free space for path '{downloadDirectory}' failed.", _configuration.DownloadDirectory);
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn der Datenträger nicht erreichbar ist.
        /// </summary>
        private void StorageOffline()
        {
            try
            {
                _logger.LogInformation("The path '{downloadDirectory}' is not accessible.", _configuration.DownloadDirectory);

                lock (this)
                {
                    // Datenträgerüberwachung beenden.
                    DestroyDiskSpaceWatcher();

                    // Dateisystemüberwachung beenden.
                    DestroyFileSystemWatcher();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to release the resources.");
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Der Dienst wird vom Server gestartet.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            monitor.Start();

            if (fileWatcher != null)
            {
                try
                {
                    _logger.LogInformation("The path '{downloadDirectory}' is being scanned for existing files.", _configuration.DownloadDirectory);

                    // Vorhandene Dateien hinzufügen.
                    foreach (string filepath in Directory.GetFiles(
                        fileWatcher.Path,
                        fileWatcher.Filter, SearchOption.TopDirectoryOnly))
                    {

                        await AddFileAsync(filepath, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "The path '{downloadDirectory}' could not be searched for existing files. The restore of the last session failed.", _configuration.DownloadDirectory);
                }
                finally
                {
                    // Monitor aktivieren.
                    fileWatcher.EnableRaisingEvents = true;
                }
            }
        }

        /// <summary>
        /// Der Dienst wird vom Server beendet.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            monitor.Stop();
            fileWatcher.Dispose();

            return Task.CompletedTask;
        }

        #endregion

        #region DiskSpaceWatcher

        /// <summary>
        /// Erstellt eine neue Datenträgerüberwachung.
        /// </summary>
        private void CreateDiskSpaceWatcher(string path)
        {
            drive = null;
            drive = new(path);

            StorageUpdate();
        }

        /// <summary>
        /// Gibt die Ressourcen frei.
        /// </summary>
        private void DestroyDiskSpaceWatcher()
        {
            _list.Clear();
            drive = null;

            // Wenn kein Datenträger gefunden wurde, Anzeige in der Benutzeroberfläche offline schalten.
            _setupBuilder.DiskInfo = new DiskSpaceRecord()
            {
                IsAvailable = false
            };

            _queue.Add(CommandFromServer.Disk, _setupBuilder.DiskInfo);
        }

        /// <summary>
        /// Alle 10 Minuten wird der verfügbare Speicherplatz Wert an die verbundenen Clients gesendet.
        /// </summary>
        private void UpdateDiskSpace()
        {
            if (drive != null && drive.IsReady)
            {
                // Gesamtgröße des Datenträgers.
                long totalBytes = drive.TotalSize;

                if (totalBytes == 0)
                {
                    return;
                }

                // Freier Speicherplatz des Datenträgers.
                long freeBytes = drive.AvailableFreeSpace;

                // Belegung des Datenträgers in Prozent.
                ushort percent = (ushort)(100 - (100 * freeBytes / totalBytes));

                // Ersten Eintrag entfernen.
                if (_list.Count != 0)
                {
                    _list.RemoveAt(0);
                }

                // Wenn Liste leer, mit aktuellen Werten auffüllen.
                while (_list.Count <= 30)
                {
                    _list.Add(percent);
                }

                // Ausgelesene Informationen in den Cache schreiben.
                _setupBuilder.DiskInfo = new DiskSpaceRecord()
                {
                    IsAvailable = true,
                    TotalSize = totalBytes,
                    FreeBytes = freeBytes,
                    Data = _list.ToArray()
                };

                _queue.Add(CommandFromServer.Disk, _setupBuilder.DiskInfo);
            }
        }

        #endregion

        #region FileSystemWatcher

        /// <summary>
        /// Erstellt eine neue Datenträgerüberwachung.
        /// Textdateien die in diesem Ordner gepspeichert werden, werden anschließend automatisch herunterladen.
        /// </summary>
        private void CreateFileSystemWatcher(string path)
        {
            // Dateisystemüberwachung einrichten.
            fileWatcher = new FileSystemWatcher(path, FileSystemMonitorFilter)
            {
                // Dateinamen und Veränderungen der Dateigröße überwachen.
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
            };
            fileWatcher.Created += OnFileChanged;
            fileWatcher.Changed += OnFileChanged;
            fileWatcher.IncludeSubdirectories = false;

        }

        /// <summary>
        /// Gibt die Ressourcen frei.
        /// </summary>
        private void DestroyFileSystemWatcher()
        {
            // Ressourcen der vorherigen Dateiüberwachung freigeben.
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Created -= OnFileChanged;
                fileWatcher.Changed -= OnFileChanged;
                fileWatcher.Dispose();
                fileWatcher = null;
            }
        }

        /// <summary>
        /// Eine gefundene Datei dem Aufgabenplaner geben.
        /// </summary>
        /// <param name="filePath">Der vollständige Pfad zur Datei.</param>
        /// <param name="autostart">Gibt an ob der Download automatisch gestartet werden soll.</param>
        private async Task AddFileAsync(string filePath, bool autostart)
        {
            try
            {
                DownloadRecord download = DownloadRecord.Load(filePath);

                // Downloads ohne Inhalt werden ignoriert.
                if (download.Files.Length == 0)
                {
                    return;
                }

                // Downloads mit gleichem Namen und Inhalt werden übersprungen.
                await _scheduler.CreateAsync(
                    download.Name,
                    download.Files,
                    download.Password,
                    autostart);

                _logger.LogDebug("The '{fileName}' file has been added.", download.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The file with path {filePath} could not be added.", filePath);
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn eine neue Datei in dem überwachten Ordner gefunden wird.
        /// </summary>
        private async void OnFileChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            try
            {
                _logger.LogDebug("FileSystemMonitor has detected a file change: {path}'", fileSystemEventArgs.FullPath);

                FileInfo fileInfo = new(fileSystemEventArgs.FullPath);

                if (fileInfo.Exists && fileInfo.Length != 0)
                {
                    await AddFileAsync(fileInfo.FullName, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileSystemMonitor could not add the file '{fileName}'.", fileSystemEventArgs.Name);

            }
        }

        #endregion
    }
}