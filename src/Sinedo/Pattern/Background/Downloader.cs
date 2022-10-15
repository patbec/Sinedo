using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sharehoster.Interfaces;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Background
{
    public class Downloader : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly DownloadScheduler scheduler;
        private readonly DownloadRepository repository;
        private readonly SetupBuilder setupBuilder;
        private readonly ILogger<Monitoring> logger;

        /// <summary>
        /// Cache mit dem Verlauf der Auslastung.
        /// </summary>
        private readonly List<ushort> monitoringCache = new ushort[30].ToList();

        public Downloader(IConfiguration configuration, DownloadScheduler scheduler, DownloadRepository repository, SetupBuilder setupBuilder, ILogger<Monitoring> logger)
        {
            this.configuration = configuration;
            this.scheduler = scheduler;
            this.repository = repository;
            this.setupBuilder = setupBuilder;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Download service started.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await ConsumeAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Canceled
            }
            finally
            {
                logger.LogInformation("Download service stopped.");
            }
        }

        public async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            string name = await scheduler.ReceiveAsync(queue: nameof(Downloader), cancellationToken);

            try
            {
                var cancellationSource = new CancellationTokenSource();

                using (await repository.Context.WriterLockAsync(cancellationToken))
                {
                    DownloadRecord download = repository.Find(name);

                    if (download.State != DownloadState.Queued)
                    {
                        return;
                    }

                    download = download with
                    {
                        State = DownloadState.Running,
                        Queue = nameof(Downloader),
                        BytesPerSecond = null,
                        SecondsToComplete = null,
                        GroupPercent = null,
                        LastException = null,
                        Cancellation = cancellationSource,
                    };

                    repository.Update(download);
                }

                cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationSource.Token).Token;

                // DoWork.
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

                await scheduler.OnCompleted(name);
            }
            catch (OperationCanceledException)
            {
                await scheduler.OnCanceled(name);
            }
            catch (Exception exception)
            {
                await scheduler.OnFailed(name, exception);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Download service stopping.");

            await base.StopAsync(stoppingToken);
        }

        public void MakeFiles(string downloadName)
        {
            //string downloadPath = Path.Combine(configuration.DownloadDirectory, Sanitizer.Sanitize(downloadName));

            // Directory.CreateDirectory(downloadPath);

            // bytesDownloaded = 0;

            // // Zieldateien erstellen und öffnen.
            // foreach (SharehosterFile link in sharehosterFiles)
            // {
            //     string sanitizedPath = Path.Combine(downloadPath, Sanitizer.Sanitize(link.Name));

            //     var fileStream = new FileStream(sanitizedPath,
            //                                     FileMode.OpenOrCreate,
            //                                     FileAccess.ReadWrite,
            //                                     FileShare.None);

            //     // Aktuellen Fortschritt berechnen, bereits heruntergeladene Bytes zählen.
            //     bytesDownloaded += fileStream.Length;

            //     fileHandles.Add(link.Uid, fileStream);
            // }
        }
    }
}