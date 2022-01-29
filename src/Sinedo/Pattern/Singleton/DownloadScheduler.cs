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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class DownloadSchedulerBase
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
        protected WebSocketBroadcaster broadcaster;

        /// <summary>
        /// Dienst für Logs.
        /// </summary>
        protected ILogger<DownloadScheduler> logger;

        /// <summary>
        /// Dienst um Dateien herunterzuladen.
        /// </summary>
        protected Sharehosters sharehosters;

        /// <summary>
        /// Tokens um Hintergrund-Tasks zu beenden.
        /// </summary>
        protected IHostApplicationLifetime lifetime;

        #endregion

        /// <summary>
        /// Aktualisiert den Zustand des angegebenen Downloads.
        /// </summary>
        /// <param name="name">Name des Downloads.</param>
        /// <param name="state">Neuer Zustand des Downloads.</param>
        /// <param name="parameter">Optionaler Parameter für den Zustand.</param>
        protected void SetState(string name, DownloadState state, object parameter = null)
        {
            DownloadRecord download = repository.Find(name);

            switch (state)
            {
                case DownloadState.Canceled:
                    {
                        download = download with
                        {
                            State = DownloadState.Canceled,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Completed:
                    {
                        download = download with
                        {
                            State = DownloadState.Completed,
                            // Meta = null,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Deleting:
                    {
                        download = download with
                        {
                            State = DownloadState.Deleting,
                            // Meta = null,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Failed:
                    {
                        download = download with
                        {
                            State = DownloadState.Failed,
                            // Meta = null,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = parameter.GetType().ToString(),
                        };

                        break;
                    }
                case DownloadState.Idle:
                    {
                        download = download with
                        {
                            State = DownloadState.Idle,
                            // Meta = null,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Queued:
                    {
                        download = download with
                        {
                            State = DownloadState.Queued,
                            // Meta = null,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Running:
                    {
                        download = download with
                        {
                            State = DownloadState.Running,
                            // Meta = actionPack.Meta,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Stopping:
                    {
                        download = download with
                        {
                            State = DownloadState.Stopping,
                            // Meta = null,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = null,
                        };

                        break;
                    }
                case DownloadState.Unsupported:
                    {
                        download = download with
                        {
                            State = DownloadState.Unsupported,
                            BytesPerSecond = null,
                            SecondsToComplete = null,
                            GroupPercent = null,
                            LastException = parameter.GetType().ToString(),
                        };

                        break;
                    }
                default:
                    {
                        throw new StateMaschineException();
                    }
            }

            repository.Update(download);
        }
    }

    public sealed class DownloadScheduler : DownloadSchedulerMonitoring
    {
        public DownloadScheduler(DownloadRepository repository, IConfiguration configuration, WebSocketBroadcaster broadcaster, ILogger<DownloadScheduler> logger, IHostApplicationLifetime lifetime)
        {
            base.repository = repository;
            base.configuration = configuration;
            base.broadcaster = broadcaster;
            base.logger = logger;
            base.lifetime = lifetime;

            // Anzahl von Tasks erstellen die in den Einstellungen hinterlegt sind.
            CreateTasks();

            // Einstellungen wurden aktualisiert. Prüfen ob neue Tasks erstellt werden sollen.
            configuration.PropertyChanged += (s, p) => CreateTasks();
        }

        public async Task<string> CreateNewDownload(string name, string[] files, string password = null, bool autostart = true)
        {
            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = AddDownloadToRepository(name, files, password);

                if (autostart)
                {
                    AddDownloadToQueue(download);
                    SetState(name, DownloadState.Queued);
                }
            }

            return name;
        }

        /// <summary>
        /// Startet alle Downloads.
        /// </summary>
        public async Task Start()
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
                                AddDownloadToQueue(download);
                                SetState(download.Name, DownloadState.Queued);
                                break;
                            }
                    }
                }
            };
        }

        public async Task Start(string name)
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
                            AddDownloadToQueue(download);
                            SetState(name, DownloadState.Queued);

                            break;
                        }
                    default: throw new CommandNotAllowedException();
                }
            };
        }

        public async Task Stop()
        {
            using (await repository.Context.WriterLockAsync())
            {
                foreach (DownloadRecord download in repository.AsEnumerable())
                {
                    switch (download.State)
                    {
                        case DownloadState.Queued:
                            {
                                RemoveDownloadFromQueue(download);
                                SetState(download.Name, DownloadState.Idle);
                                break;
                            }
                        case DownloadState.Running:
                            {
                                SetState(download.Name, DownloadState.Stopping);
                                CancelDownload(download.Name);
                                break;
                            }
                    }
                }
            };
        }

        public async Task Stop(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                DownloadRecord download = repository.Find(name);

                switch (download.State)
                {
                    case DownloadState.Queued:
                        {
                            RemoveDownloadFromQueue(download);
                            SetState(name, DownloadState.Idle);
                            break;
                        }
                    case DownloadState.Running:
                        {
                            SetState(name, DownloadState.Stopping);
                            CancelDownload(name);
                            break;
                        }
                    default: throw new CommandNotAllowedException();
                }
            };
        }

        public async Task Delete(string name)
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
    }
}