using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public abstract class DownloadSchedulerMonitoring : DownloadSchedulerFunctions
    {
        /// <summary>
        /// Cache mit dem Verlauf der Auslastung.
        /// </summary>
        private readonly List<ushort> monitoringCache = new()
        {
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
        };

        #region Properties

        /// <summary>
        /// Stellt anderen Diensten den Verlauf der Auslastung bereit.
        /// </summary>
        public BandwidthRecord BandwidthInfo { get; private set; }

        #endregion

        public DownloadSchedulerMonitoring()
        {
            BandwidthInfo = new()
            {
                BytesReadTotal = 0,
                BytesRead = 0,
                Data = monitoringCache.ToArray(),
            };

            // Berechnet jede Sekunde die aktuelle Download-Geschwindigkeit.
            _ = new Timer(OnUpdate, null, 1000, 1000);
        }

        /// <summary>
        /// Aktualisiert die Download-Geschwindigkeit.
        /// </summary>
        private async void OnUpdate(object timerState)
        {
            try
            {
                long bytesReadCurrent = 0;

                // Download-Geschwindigkeit etc. berechnen. 
                using (await repository.Context.WriterLockAsync())
                {
                    foreach (Downloader downloader in CurrentDownloads)
                    {
                        var bytesRead = downloader.Monitoring.Update();
                        var monitoring = downloader.Monitoring;

                        // Download Informationen aktualisieren.
                        var download = repository.Find(downloader.Name) with
                        {
                            State = DownloadState.Running,
                            LastException = null,
                            BytesPerSecond = monitoring?.BytesPerSecond,
                            SecondsToComplete = monitoring?.SecondsToComplete,
                            GroupPercent = monitoring?.Percent
                        };
                        repository.Update(download);

                        bytesReadCurrent += bytesRead;
                        // Die Geschwindigkeit nur für Downloads berechnen.
                        // if (download.Meta == GroupMeta.Download)
                        // {
                        //     bytesReadCurrent += bytesRead;
                        // }
                    }
                };

                // Neue Statusinformationen veröffentlichen.
                PublishStats(bytesReadCurrent);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Calculation of the progress has failed.");
            }
        }

        /// <summary>
        /// Veröffentlicht die Auslastung.
        /// </summary>
        private void PublishStats(long bytesRead)
        {
            // Prüfen ob das Array leer ist, dann kein Paket senden.
            bool sendPackage = monitoringCache.Any(s => s != 0);

            long bytesReadTotal = BandwidthInfo.BytesReadTotal + bytesRead;
            long totalBandwidth = configuration.InternetConnectionInMbits * 125000;

            double utilization = (bytesRead * 100) / totalBandwidth;
            ushort utilizationPercent = (ushort)utilization;

            monitoringCache.RemoveAt(0);
            monitoringCache.Add(utilizationPercent);

            BandwidthInfo = new BandwidthRecord()
            {
                BytesReadTotal = bytesReadTotal,
                BytesRead = bytesRead,
                Data = monitoringCache.ToArray()
            };

            if (sendPackage)
            {
                broadcaster.Add(CommandFromServer.Bandwidth, BandwidthInfo);
            }
        }
    }
}