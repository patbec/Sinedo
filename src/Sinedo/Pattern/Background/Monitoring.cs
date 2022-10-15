using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Background
{
    // public class Monitoring : BackgroundService
    // {
    //     private readonly IConfiguration configuration;
    //     private readonly DownloadScheduler scheduler;
    //     private readonly DownloadRepository repository;
    //     private readonly BroadcastQueue queue;
    //     private readonly SetupBuilder setupBuilder;
    //     private readonly ILogger<Monitoring> logger;

    //     /// <summary>
    //     /// Cache mit dem Verlauf der Auslastung.
    //     /// </summary>
    //     private readonly List<ushort> monitoringCache = new ushort[30].ToList();

    //     public Monitoring(IConfiguration configuration, DownloadScheduler scheduler, DownloadRepository repository, SetupBuilder setupBuilder, ILogger<Monitoring> logger)
    //     {
    //         this.configuration = configuration;
    //         this.scheduler = scheduler;
    //         this.repository = repository;
    //         this.setupBuilder = setupBuilder;
    //         this.logger = logger;
    //     }


    //     /// <summary>
    //     /// Berechnet jede Sekunde die aktuelle Download-Geschwindigkeit.
    //     /// </summary>
    //     /// <param name="stoppingToken"></param>
    //     /// <returns></returns>
    //     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //     {
    //         // Standardwerte setzen.
    //         setupBuilder.BandwidthInfo = new()
    //         {
    //             BytesReadTotal = 0,
    //             BytesRead = 0,
    //             Data = monitoringCache.ToArray(),
    //         };

    //         try
    //         {
    //             logger.LogInformation("Monitoring service started.");

    //             using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

    //             while (true)
    //             {
    //                 await OnUpdate();
    //                 await timer.WaitForNextTickAsync(stoppingToken);
    //             }
    //         }
    //         catch (OperationCanceledException)
    //         {
    //             // Canceled
    //         }
    //         finally
    //         {
    //             logger.LogInformation("Monitoring service stopped.");
    //         }
    //     }

    //     /// <summary>
    //     /// Aktualisiert die Download-Geschwindigkeit.
    //     /// </summary>
    //     private async Task OnUpdate()
    //     {
    //         try
    //         {
    //             long bytesReadCurrent = 0;

    //             // Download-Geschwindigkeit etc. berechnen. 
    //             using (await repository.Context.WriterLockAsync())
    //             {
    //                 foreach (Downloader downloader in scheduler.RunningDownloads)
    //                 {
    //                     var bytesRead = downloader.Monitoring.Update();
    //                     var monitoring = downloader.Monitoring;

    //                     // Download Informationen aktualisieren.
    //                     var download = repository.Find(downloader.Name) with
    //                     {
    //                         State = DownloadState.Running,
    //                         LastException = null,
    //                         BytesPerSecond = monitoring?.BytesPerSecond,
    //                         SecondsToComplete = monitoring?.SecondsToComplete,
    //                         GroupPercent = monitoring?.Percent
    //                     };
    //                     repository.Update(download);

    //                     bytesReadCurrent += bytesRead;
    //                     // Die Geschwindigkeit nur für Downloads berechnen.
    //                     // if (download.Meta == GroupMeta.Download)
    //                     // {
    //                     //     bytesReadCurrent += bytesRead;
    //                     // }
    //                 }
    //             };

    //             // Neue Statusinformationen veröffentlichen.
    //             Publish(bytesReadCurrent);
    //         }
    //         catch (Exception exception)
    //         {
    //             logger.LogCritical(exception, "Calculation of the progress has failed.");
    //         }
    //     }

    //     /// <summary>
    //     /// Veröffentlicht die Auslastung.
    //     /// </summary>
    //     private void Publish(long bytesRead)
    //     {
    //         long bytesReadTotal = setupBuilder.BandwidthInfo.BytesReadTotal + bytesRead;
    //         long totalBandwidth = configuration.InternetConnectionInMbits * 125000;

    //         // Prüfen ob ein Paket gesendet werden muss.
    //         if (bytesReadTotal == 0 && monitoringCache.All(value => value == 0))
    //         {
    //             return;
    //         }

    //         double utilization = (bytesRead * 100) / totalBandwidth;
    //         ushort utilizationPercent = (ushort)utilization;

    //         monitoringCache.RemoveAt(0);
    //         monitoringCache.Add(utilizationPercent);

    //         setupBuilder.BandwidthInfo = new BandwidthRecord()
    //         {
    //             BytesReadTotal = bytesReadTotal,
    //             BytesRead = bytesRead,
    //             Data = monitoringCache.ToArray()
    //         };

    //         var package = new WebSocketPackage(CommandFromServer.Bandwidth, setupBuilder.BandwidthInfo);

    //         queues.BroadcastQueue.Post(package);
    //     }
    // }
}