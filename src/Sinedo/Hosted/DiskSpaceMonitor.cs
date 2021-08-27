using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
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
        private readonly Timer _timer;
        private readonly DiskSpaceHelper _diskSpaceHelper;
        private readonly WebSocketBroadcaster _broadcaster;
        private readonly Configuration _configuration;

        private DriveInfo drive;

        public DiskSpaceMonitor(DiskSpaceHelper diskSpaceHelper, WebSocketBroadcaster broadcaster, Configuration configuration)
        {
            _list = new();
            _timer = new(Update, null, Timeout.Infinite, Timeout.Infinite);
            _diskSpaceHelper = diskSpaceHelper;
            _broadcaster = broadcaster;
            _configuration = configuration;

            drive = new (_configuration.DownloadDirectory);

            configuration.RegisterForUpdates(() => {
                lock(drive)
                {
                    drive = new (_configuration.DownloadDirectory);
                }
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Daten sammeln und den Cache befüllen.
            Update(null);

            // Nächstes Update in 1 Minute.
            _timer.Change(60000, 60000);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
             await _timer.DisposeAsync();
        }

        private void Update(object state)
        {
            if (_list.Count >= 30) {
                _list.RemoveAt(0);
            }

            long totalBytes = 0;
            long freeBytes = 0;   

            lock(drive)
            {
                totalBytes = drive.TotalSize;
                freeBytes = drive.AvailableFreeSpace;
            }

            var freePercent = (ushort)(100 - (100 * freeBytes / totalBytes));

            // Wenn Liste leer, mit aktuellen Werten auffüllen.
            while (_list.Count <= 30)
            {
                _list.Add(freePercent);
            }

            _diskSpaceHelper.DiskInfo = new DiskSpaceRecord()
            {
                TotalSize = totalBytes,
                FreeBytes = freeBytes,
                Data = _list.ToArray()
            };

            _broadcaster.Add(CommandFromServer.DiskInfo, WebSocketPackage.PARAMETER_UNSET, _diskSpaceHelper.DiskInfo);
        }
    }
}