
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public partial class DownloadScheduler
    {
        /// <summary>
        /// Aktualisiert den Zustand des angegebenen Downloads.
        /// </summary>
        /// <param name="name">Name des Downloads.</param>
        /// <param name="state">Neuer Zustand des Downloads.</param>
        /// <param name="parameter">Optionaler Parameter für den Zustand.</param>
        private void SetState(string name, DownloadState state, object parameter = null)
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
                            Cancellation = null
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
                            Cancellation = null
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
                            Cancellation = null
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
                            Cancellation = null
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
                            Cancellation = null
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
                            Cancellation = null
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
                            Cancellation = new()
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
                            Cancellation = null
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
                            Cancellation = null
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

        public async Task OnCompleted(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Completed);
            }
        }

        public async Task OnFailed(string name, Exception exception)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Failed, exception);
            }
        }

        public async Task OnCanceled(string name)
        {
            using (await repository.Context.WriterLockAsync())
            {
                SetState(name, DownloadState.Canceled);
            }
        }
    }
}