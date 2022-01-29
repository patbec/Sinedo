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
    public abstract class DownloadSchedulerWorker : DownloadSchedulerCreate
    {
        /// <summary>
        /// Liste mit aktiven Tasks die eine gemeinsame Warteschlange abarbeiten.
        /// </summary>
        private readonly List<Task> tasks = new();

        /// <summary>
        /// Warteschlange mit Downloads.
        /// </summary>
        private readonly BufferBlock<string> queue = new();

        /// <summary>
        /// Liste mit Downloads die aktuell heruntergeladen werden.
        /// </summary>
        private readonly Dictionary<string, Downloader> currentDownloads = new();

        /// <summary>
        /// Liste mit Downloads die aktuell heruntergeladen werden.
        /// </summary>
        protected Downloader[] CurrentDownloads => currentDownloads.Values.ToArray();

        public DownloadSchedulerWorker()
        {
        }

        protected void CreateTasks()
        {
            lock (tasks)
            {
                var requestedTasks = configuration.ConcurrentDownloads;

                // Threads erstellen bis die gewünschte Anzahl gestartet wurden.
                while (tasks.Count < requestedTasks)
                {
                    tasks.Add(Task.Run(Consumer));
                    logger.LogDebug("A new consumer has been created.");
                }
            }
        }

        private bool IsTaskCountExceeded()
        {
            lock (tasks)
            {
                // Prüfen ob mehr Tasks laufen wie der Nutzer eingestellt hat.
                return tasks.Count > configuration.ConcurrentDownloads;
            }
        }

        private async Task Consumer()
        {
            Guid uid = Guid.NewGuid();

            try
            {
                logger.LogInformation("Consumer {uid} running.", uid);

                while (await queue.OutputAvailableAsync(lifetime.ApplicationStopping))
                {
                    // Prüfen ob mehr Tasks laufen wie der Nutzer eingestellt hat.
                    if (IsTaskCountExceeded()) break;

                    string name = null;

                    using (await repository.Context.WriterLockAsync())
                    {
                        bool isSuccessfullyReceived = queue.TryReceive(out name);

                        if (isSuccessfullyReceived == false)
                        {
                            continue;
                        }

                        SetState(name, DownloadState.Running);
                    }

                    logger.LogInformation("Consumer {uid} processed item {name}.", uid, name);

                    await ProcessItem(name);


                    // if (name != null)
                    // {
                    //     logger.LogInformation("Consumer {0} processing item {1}.", uid, name);

                    //     await ProcessItem(name);
                    // }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Consumer {uid} failed.", uid);
            }
            finally
            {
                logger.LogInformation("Consumer {uid} stopped.", uid);
            }
        }

        protected void AddDownloadToQueue(DownloadRecord download)
        {
            // Aufgabe zur Warteschlange hinzufügen.
            bool isAdded = queue.Post(download.Name);

            if (!isAdded)
            {
                throw new StateMaschineException();
            }
        }

        protected void RemoveDownloadFromQueue(DownloadRecord download)
        {
            bool isRemoved = queue.TryReceive(o => o == download.Name, out string removedDownload);

            if (!isRemoved)
            {
                throw new StateMaschineException();
            }
        }

        private async Task ProcessItem(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            try
            {
                using (Downloader downloader = await CreateDownloader(name))
                {
                    await Task.Delay(10000);
                    // await downloader.GetFileInfosFromApi();
                    // await downloader.MakeFiles();

                    // if (!downloader.IsDownloadCompleted)
                    // {
                    //     OnRunningDownload(name);

                    //     // Bei Internetproblemen: 30 Versuche â 30 Sekunden
                    //     await RetryIfConnectionLost(30, 30, () => downloader.Download(), downloader.cancellationTokenSource.Token);
                    // }


                }

                await OnCompleted(name);
            }
            catch (OperationCanceledException)
            {
                await OnCanceled(name);
            }
            catch (Exception ex)
            {
                if (ex is AggregateException)
                {
                    ex = ex.InnerException;
                }

                await OnFailed(name, ex);
            }
        }

        private async Task<Downloader> CreateDownloader(string name)
        {
            string sanitizedPath = SantanizePath(configuration.DownloadDirectory, name);

            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = repository.Find(name);

                Downloader downloader = new(sharehosters, name, sanitizedPath, download.Files);
                currentDownloads.Add(name, downloader);

                return downloader;
            }
        }

        // private async Task RetryIfConnectionLost(string name, int count, int delay, Action callback, CancellationToken cancellationToken)
        // {
        //     while (count != 0)
        //     {
        //         try
        //         {
        //             callback();
        //             return;
        //         }
        //         catch (IOException ie)
        //         {
        //             count--;

        //             await OnRetry(name);
        //             if (count == 0)
        //             {
        //                 throw;
        //             }

        //             logger.LogWarning(ie, $"A connection problem occurred while downloading {name}.");
        //             // ToDo: Log
        //         }

        //         await Task.Delay(delay * 1000, cancellationToken);
        //         cancellationTokenSource.Token.ThrowIfCancellationRequested();

        //         await OnRunning(name);
        //     }
        // }

        // private async Task OnRetry(string name)
        // {
        //     using (await repository.Context.WriterLockAsync())
        //     {
        //         SetState(name, DownloadState.Running);
        //     }
        // }

        private async Task OnRunning(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Running);
            }
        }

        private async Task OnCompleted(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Completed);
            }
        }

        private async Task OnFailed(string name, Exception exception)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Failed, exception);
            }
        }

        private async Task OnCanceled(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Canceled);
            }
        }

        /// <summary>
        /// Entfernt ungültige Zeichen aus dem Dateinamen.
        /// </summary>
        public static string SantanizePath(string path, string fileName)
        {
            fileName = Sanitizer.Sanitize(fileName);

            return Path.Combine(path, fileName);
        }
    }
}