using System;
using System.Collections;
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
    public class DiskSpaceHelper
    {
        #region Properties

        public DiskSpaceRecord DiskInfo { get; set; }

        #endregion
    }

    public class StorageService : IHostedService
    {
        private readonly List<ushort> _list;
        private readonly DiskSpaceHelper _diskSpaceHelper;
        private readonly WebSocketBroadcaster _broadcaster;
        private readonly DownloadScheduler _scheduler;
        private readonly Configuration _configuration;
        private readonly ILogger<StorageService> _logger;

        private DriveInfo _drive;
        private StorageMonitor _monitor;
        private FileSystemWatcher fileWatcher;

        #region Constants

        /// <summary>
        /// Filter für Dateien die überwacht werden sollen.
        /// </summary>
        private const string FileSystemMonitorFilter = "*.txt";

        #endregion

        public StorageService(DiskSpaceHelper diskSpaceHelper, WebSocketBroadcaster broadcaster, DownloadScheduler scheduler, Configuration configuration, ILogger<StorageService> logger)
        {
            _list   = new();

            _diskSpaceHelper    = diskSpaceHelper;
            _broadcaster        = broadcaster;
            _scheduler          = scheduler;
            _configuration      = configuration;
            _logger             = logger;

            _monitor = new (configuration.DownloadDirectory);
            _monitor.StorageOnline += StorageOnline;
            _monitor.StorageUpdate += StorageUpdate;
            _monitor.StorageOffline += StorageOffline;

            configuration.RegisterForUpdates(() => {
                lock(this) {
                    _logger.LogInformation("Settings changed, new path is set.");
                    _monitor.Stop();
                    _monitor.StorageOnline -= StorageOnline;
                    _monitor.StorageUpdate -= StorageUpdate;
                    _monitor.StorageOffline -= StorageOffline;

                    _monitor = new (configuration.DownloadDirectory);
                    _monitor.StorageOnline += StorageOnline;
                    _monitor.StorageUpdate += StorageUpdate;
                    _monitor.StorageOffline += StorageOffline;
                    _monitor.Start();
                }
            });
        }

        #region Events

        /// <summary>
        /// Wird aufgerufen wenn der Datenträger verfügbar ist.
        /// </summary>
        private void StorageOnline()
        {
            try
            {
                lock(this)
                {
                    _logger.LogInformation("The path '{0}' is available.", _configuration.DownloadDirectory);

                    // Datenträgerüberwachung einrichten.
                    CreateDiskSpaceWatcher(_configuration.DownloadDirectory);

                    // Dateisystemüberwachung einrichten.
                    CreateFileSystemWatcher(_configuration.DownloadDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"The specified path '{_configuration.DownloadDirectory}' cannot be monitored for changes.");
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn der Datenträger aktualisiert werden soll.
        /// </summary>
        private void StorageUpdate()
        {
            lock(this)
            {
                try
                {
                    // Im Speicherplatz-Verlauf einen neuen Wert hinzufügen und an verbundene Clients senden.
                    UpdateDiskSpace();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Calculation of free space for path '{_configuration.DownloadDirectory}' failed.");
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn der Datenträger nicht erreichbar ist.
        /// </summary>
        private void StorageOffline()
        {
            _logger.LogInformation("The path '{0}' is not accessible.", _configuration.DownloadDirectory);

            // Datenträgerüberwachung beenden.
            DestroyDiskSpaceWatcher();

            // Dateisystemüberwachung beenden.
            DestroyFileSystemWatcher();
        }

        #endregion

        #region Public

        /// <summary>
        /// Der Dienst wird vom Server gestartet.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            lock(this)
            {
                _logger.LogInformation("The path '{0}' is being scanned for existing files.", _configuration.DownloadDirectory);
                
                _monitor.Start();

                if(fileWatcher != null)
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
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Der Dienst wird vom Server beendet.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            lock(this) {
                _monitor.Stop();
            }

            return Task.CompletedTask;
        }

        #endregion

        #region DiskSpaceWatcher

        /// <summary>
        /// Erstellt eine neue Datenträgerüberwachung.
        /// </summary>
        private void CreateDiskSpaceWatcher(string path)
        {
            _drive = null;
            _drive = new (path);

            StorageUpdate();
        }

        /// <summary>
        /// Gibt die Ressourcen frei.
        /// </summary>
        private void DestroyDiskSpaceWatcher()
        {
            _list.Clear();
            _drive = null;

            // Wenn kein Datenträger gefunden wurde, Anzeige in der Benutzeroberfläche offline schalten.
            _diskSpaceHelper.DiskInfo = new DiskSpaceRecord()
            {
                IsAvailable = false
            };

            _broadcaster.Add(CommandFromServer.DiskInfo, WebSocketPackage.PARAMETER_UNSET, _diskSpaceHelper.DiskInfo);
        }

        /// <summary>
        /// Alle 10 Minuten wird der verfügbare Speicherplatz Wert an die verbundenen Clients gesendet.
        /// </summary>
        private void UpdateDiskSpace()
        {
            if(_drive != null && _drive.IsReady)
            {
                // Gesamtgröße des Datenträgers.
                long totalBytes = _drive.TotalSize;

                if(totalBytes == 0) {
                    return;
                }

                // Freier Speicherplatz des Datenträgers.
                long freeBytes = _drive.AvailableFreeSpace;

                // Belegung des Datenträgers in Prozent.
                ushort percent = (ushort)(100 - (100 * freeBytes / totalBytes));

                // Ersten Eintrag entfernen.
                if( _list.Count != 0) {
                    _list.RemoveAt(0);
                }

                // Wenn Liste leer, mit aktuellen Werten auffüllen.
                while (_list.Count <= 30) {
                    _list.Add(percent);
                }

                // Ausgelesene Informationen in den Cache schreiben.
                _diskSpaceHelper.DiskInfo = new DiskSpaceRecord()
                {
                    IsAvailable = true,
                    TotalSize = totalBytes,
                    FreeBytes = freeBytes,
                    Data = _list.ToArray()
                };

                _broadcaster.Add(CommandFromServer.DiskInfo, WebSocketPackage.PARAMETER_UNSET, _diskSpaceHelper.DiskInfo);         
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
            fileWatcher.Created += OnCreated;
            fileWatcher.Changed += OnCreated;
            fileWatcher.IncludeSubdirectories = false;

        }

        /// <summary>
        /// Gibt die Ressourcen frei.
        /// </summary>
        private void DestroyFileSystemWatcher()
        {
            // Ressourcen der vorherigen Dateiüberwachung freigeben.
            if (fileWatcher != null) {
                fileWatcher.Dispose();
                fileWatcher = null;
            }
        }

        /// <summary>
        /// Eine gefundene Datei hinzufügen.
        /// </summary>
        /// <param name="filepath">Der vollständige Pfad zur Datei.</param>
        private void AddToDownloads(string filepath, bool autostart)
        {
            try
            {
               
                string filename = Path.GetFileNameWithoutExtension(filepath);
                string[] files = File.ReadAllLines(filepath);

                if(files.Any()) {
                    _scheduler.Create(filename, files, autostart, skipIfContains: true);
                }

                _logger.LogInformation("The '{0}' file has been added.", filename);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"The file with path {filepath} could not be added.");
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn eine neue Datei in dem überwachten Ordner gefunden wird.
        /// </summary>
        private void OnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            lock(this)
            {
                switch(fileSystemEventArgs.ChangeType) {
                    case WatcherChangeTypes.Created:
                    {
                        _logger.LogDebug("FileSystemMonitor: '{0}'", fileSystemEventArgs.FullPath);

                        try {
                            FileInfo fileInfo = new (fileSystemEventArgs.FullPath);

                            if (fileInfo.Exists && fileInfo.Length != 0) {
                                AddToDownloads(fileInfo.FullName, true);
                            }
                        } catch (Exception ex) {
                            _logger.LogError(ex, "FileSystemMonitor could not add the file '{0}'.", fileSystemEventArgs.Name);

                        }
                        break;
                    }
                }
            }
        }

        #endregion
    }
}