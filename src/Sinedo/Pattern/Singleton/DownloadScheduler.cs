using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Background;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public partial class DownloadScheduler
    {
        #region Dependency Services

        /// <summary>
        /// Dienst für den Zugriff auf die Datenbank.
        /// </summary>
        protected DownloadRepository repository;

        /// <summary>
        /// Dienst für den Zugriff auf die Benutzereinstellungen.
        /// </summary>
        protected IConfiguration configuration;

        /// <summary>
        /// Dienst um Benachrichtigungen an verbundene Clients zu senden.
        /// </summary>
        protected BroadcastQueue queue;

        /// <summary>
        /// Dienst für Logs.
        /// </summary>
        protected ILogger<DownloadScheduler> logger;

        #endregion

        /// <summary>
        /// Warteschlange mit Downloads.
        /// </summary>
        private readonly BufferBlock<string> queueDownload = new();

        public DownloadScheduler(DownloadRepository repository, IConfiguration configuration, BroadcastQueue queue, ILogger<DownloadScheduler> logger)
        {
            this.repository = repository;
            this.configuration = configuration;
            this.queue = queue;
            this.logger = logger;
        }

        public async Task<string> CreateAsync(string name, string[] files, string password = null, bool autostart = true)
        {
            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = AddDownloadToRepository(name, files, password);

                if (autostart)
                {
                    queueDownload.Post(download.Name);
                    SetState(name, DownloadState.Queued);
                }
            }

            return name;
        }

        /// <summary>
        /// Startet alle Downloads.
        /// </summary>
        public async Task StartAllAsync()
        {
            using (await repository.Context.WriterLockAsync())
            {
                foreach (DownloadRecord download in repository.AsEnumerable())
                {
                    switch (download.State)
                    {
                        case DownloadState.Canceled:
                        case DownloadState.Failed:
                        case DownloadState.Idle:
                            {
                                queueDownload.Post(download.Name);
                                SetState(download.Name, DownloadState.Queued);
                                break;
                            }
                    }
                }
            };
        }

        public async Task StartAsync(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = repository.Find(name);

                switch (download.State)
                {
                    case DownloadState.Canceled:
                    case DownloadState.Failed:
                    case DownloadState.Idle:
                    case DownloadState.Completed:
                        {
                            queueDownload.Post(download.Name);
                            SetState(name, DownloadState.Queued);

                            break;
                        }
                    default: throw new CommandNotAllowedException();
                }
            };
        }

        public async Task StopAllAsync()
        {
            using (await repository.Context.WriterLockAsync())
            {
                foreach (DownloadRecord download in repository.AsEnumerable()) // ToDo: Nach Datum sortieren
                {
                    switch (download.State)
                    {
                        case DownloadState.Queued:
                            {
                                SetState(download.Name, DownloadState.Idle);
                                break;
                            }
                        case DownloadState.Running:
                            {
                                SetState(download.Name, DownloadState.Stopping);
                                download.Cancellation.Cancel();
                                break;
                            }
                    }
                }
            }
        }

        public async Task StopAsync(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = repository.Find(name);

                switch (download.State)
                {
                    case DownloadState.Queued:
                        {
                            SetState(name, DownloadState.Idle);
                            break;
                        }
                    case DownloadState.Running:
                        {
                            SetState(name, DownloadState.Stopping);
                            download.Cancellation.Cancel();
                            break;
                        }
                    default: throw new CommandNotAllowedException();
                }
            }
        }

        public async Task DeleteAsync(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = repository.Find(name);

                switch (download.State)
                {
                    case DownloadState.Completed:
                    case DownloadState.Canceled:
                    case DownloadState.Failed:
                    case DownloadState.Idle:
                        {
                            SetState(name, DownloadState.Deleting);
                            DeleteFolderInBackground(name);
                            break;
                        }
                    default: throw new CommandNotAllowedException();
                }
            };
        }

        public Task<string> ReceiveAsync(string queue, CancellationToken cancellationToken)
        {
            switch (queue)
            {
                case nameof(Downloader):
                    {
                        return queueDownload.ReceiveAsync(cancellationToken);
                    }
                default: throw new NotImplementedException("The requested queue does not exist.");
            }
        }
    }
}