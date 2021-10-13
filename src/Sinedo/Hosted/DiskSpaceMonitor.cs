using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Hosted
{
    public class DiskSpaceHelper
    {
        #region Properties

        public DiskSpaceRecord DiskInfo { get; set; }

        #endregion
    }

    public class DiskSpaceMonitor : IHostedService
    {
        private readonly List<ushort> _list;
        private readonly DiskSpaceHelper _diskSpaceHelper;
        private readonly WebSocketBroadcaster _broadcaster;
        private readonly Configuration _configuration;
        private readonly ILogger<DiskSpaceMonitor> _logger;

        private DriveInfo _drive;
        private StorageMonitor _monitor;

        public DiskSpaceMonitor(DiskSpaceHelper diskSpaceHelper, WebSocketBroadcaster broadcaster, Configuration configuration, ILogger<DiskSpaceMonitor> logger)
        {
            _list   = new();

            _diskSpaceHelper    = diskSpaceHelper;
            _broadcaster        = broadcaster;
            _configuration      = configuration;
            _logger             = logger;

            _monitor = new (configuration.DownloadDirectory);
            _monitor.StorageOnline += StorageOnline;
            _monitor.StorageUpdate += StorageUpdate;
            _monitor.StorageOffline += StorageOffline;

            configuration.RegisterForUpdates(() => {
                lock(this) {
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

        private void StorageOnline()
        {
            try
            {
                lock(this)
                {
                    _drive = null;
                    _drive = new (_configuration.DownloadDirectory);

                    StorageUpdate();
                }
            } catch (Exception ex) {
                _logger.LogError(ex, $"The specified path '{_configuration.DownloadDirectory}' cannot be monitored for disk space.");
            }
        }

        private void StorageUpdate()
        {
            try
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
            } catch (Exception ex) {
                _logger.LogError(ex, $"Calculation of free space for path '{_configuration.DownloadDirectory}' x failed.");
            }
        }

        private void StorageOffline()
        {
            _list.Clear();
            _drive = null;

             // Wenn kein Datenträger gefunden wurde, Anzeige in der Benutzeroberfläche Offline schalten.
            _diskSpaceHelper.DiskInfo = new DiskSpaceRecord()
            {
                IsAvailable = false
            };

            _broadcaster.Add(CommandFromServer.DiskInfo, WebSocketPackage.PARAMETER_UNSET, _diskSpaceHelper.DiskInfo);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            lock(this) {
                _monitor.Start();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            lock(this) {
                _monitor.Stop();
            }

            return Task.CompletedTask;
        }
    }
}