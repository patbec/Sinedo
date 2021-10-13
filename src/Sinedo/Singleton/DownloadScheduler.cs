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
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Components.Common;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    /// <summary>
    /// Kann das Herunterladen von Downloads planen und durchführen.
    /// </summary>
    public class DownloadScheduler
    {
        #region Download Status

        /// <summary>
        /// Berechnet den Download-Fortschritt etc.
        /// </summary>
        private readonly Timer monitoringTimer;

        /// <summary>
        /// Cache mit dem Verlauf der Auslastung.
        /// </summary>
        private readonly List<ushort> monitoringCache = new()
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };

        #endregion

        #region Background Worker

        /// <summary>
        /// Liste mit aktiven Threads die eine gemeinsame Warteschlange abarbeiten.
        /// </summary>
        private readonly List<Thread> threads = new();

        /// <summary>
        /// Warteschlange mit Downloads.
        /// </summary>
        private readonly List<string> queue = new();

        /// <summary>
        /// Signalisiert wartende Threads, wenn neue Elemente zur Warteschlange hinzugefügt werden. 
        /// </summary>
        private readonly ManualResetEvent queueChangedEvent = new(false);

        /// <summary>
        /// Liste mit Downloads die aktuell heruntergeladen werden.
        /// </summary>
        private readonly Dictionary<string, DownloadManager> downloads = new();

        #endregion

        #region Dependency Services

        /// <summary>
        /// Dienst für den Zugriff auf die Datenbank.
        /// </summary>
        private readonly DownloadRepository repository;
        /// <summary>
        /// Dienst für den Zugriff auf die Benutzereinstellungen.
        /// </summary>
        private readonly Configuration configuration;
        /// <summary>
        /// Dienst um Benachrichtigungen an verbundene Clients zu senden.
        /// </summary>
        private readonly WebSocketBroadcaster broadcaster;
        /// <summary>
        /// Dienst für Logs.
        /// </summary>
        private readonly ILogger<DownloadScheduler> logger;

        #endregion

        #region Properties

        /// <summary>
        /// Stellt anderen Diensten den Verlauf der Auslastung bereit.
        /// </summary>
        public BandwidthRecord BandwidthInfo { get; private set; }

        #endregion

        public DownloadScheduler(DownloadRepository repository, WebSocketBroadcaster broadcaster, Configuration configuration, ILogger<DownloadScheduler> logger)
        {
            this.repository = repository;
            this.configuration = configuration;
            this.broadcaster = broadcaster;
            this.logger = logger;

            BandwidthInfo = new () {
                
                BytesReadTotal = 0,
                BytesRead = 0,
                Data = monitoringCache.ToArray(),
            };

            // Anzahl von Threads erstellen die in den Einstellungen hinterlegt sind.
            CreateThreads();

            // Einstellungen wurden aktualisiert. Prüfen ob neue Threads erstellt werden sollen.
            configuration.RegisterForUpdates(() => CreateThreads());

            monitoringTimer = new Timer(OnUpdate, null, 1000, 1000);
        }

        /// <summary>
        /// Aktualisiert die Download-Geschwindigkeit.
        /// </summary>
        private void OnUpdate(object timerState)
        {
            try {
                long bytesReadCurrent = 0;

                // Download-Geschwindigkeit etc. berechnen. 
                repository.EnterWriteLock(() => {
                    foreach (var item in downloads)
                    {
                        var manager     = item.Value;
                        var bytesRead   = manager.Update();
                        var monitoring  = manager.Monitoring;

                        // Download Informationen aktualisieren.
                        var download = repository.Find(item.Key) with
                        {
                            State               = GroupState.Running,
                            LastException       = null,
                            BytesPerSecond      = monitoring?.BytesPerSecond,
                            SecondsToComplete   = monitoring?.SecondsToComplete,
                            GroupPercent        = monitoring?.Percent
                        };
                        repository.Update(download);

                        // Die Geschwindigkeit nur für Downloads berechnen.
                        if(download.Meta == GroupMeta.Download) {
                            bytesReadCurrent += bytesRead;
                        }
                    }
                });

                // Neue Statusinformationen veröffentlichen.
                PublishStats(bytesReadCurrent);
            }
            catch (Exception exception) {
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

            if (sendPackage) {
                broadcaster.Add(CommandFromServer.BandwidthInfo, WebSocketPackage.PARAMETER_UNSET, BandwidthInfo);
            }
        }

        /// <summary>
        /// Erstellt die gewünschte Anzahl von Threads.
        /// </summary>
        public void CreateThreads()
        {
            repository.EnterReadLock(() =>
            {
                var requestedThreads = configuration.ConcurrentDownloads;

                // Threads erstellen bis die gewünschte Anzahl gestartet wurden.
                while(threads.Count < requestedThreads) {
                    Thread additionalThread = new (Worker);
                    additionalThread.Priority = ThreadPriority.BelowNormal;
                    threads.Add(additionalThread);  
                    additionalThread.Start();

                    logger.LogDebug("A new background work thread has been created.");
                }
            });
        }

        /// <summary>
        /// Arbeitet die gemeinsame Warteschlange ab.
        /// </summary>
        private void Worker()
        {
            Thread.CurrentThread.Name = "Worker";

            bool shutdownThread = false;

            do {
                queueChangedEvent.WaitOne(Timeout.Infinite);
                DownloadRecord download = null;

                repository.EnterWriteLock(() =>
                {
                    // Prüfen ob mehr Threads laufen wie der Nutzer eingestellt hat.
                    if (threads.Count > configuration.ConcurrentDownloads) {
                        shutdownThread = true;
                    }
                    else
                    {
                        var queueItem = queue.FirstOrDefault();

                        if (queueItem == null) {
                            queueChangedEvent.Reset(); // Signalisieren das keine Objekte vorhanden sind.
                        } else {
                            queue.Remove(queueItem);
                            download = repository.Find(queueItem);
                            download = SetState(GroupState.Running, download);
                        }
                    }
                });
                
                bool itemFound = (download != null);
                if (itemFound)
                {
                    OnDownload(download);
                }
            } while( ! shutdownThread);

            logger.LogDebug("Background work thread is terminated.");
        }

        /// <summary>
        /// Lädt den angegebenen Download herunter.
        /// </summary>
        private void OnDownload(DownloadRecord download)
        {
            Exception exception = null;
            DownloadManager manager = new (configuration.SharehosterUsername,
                                           configuration.SharehosterPassword, download);

            // Zu den aktiven Downloads hinzufügen.
            repository.EnterWriteLock(() => downloads.Add(download.Name, manager));

            try
            {
                // Statusupdates über Fehlermeldungen oder neuen Unterzuständen.
                manager.Status += OnStatusUpdate;
                
                // Lädt alle Dateien in der Gruppe herunter; kann sehr lange dauern.
                manager.DownloadTo(configuration.DownloadDirectory);

                // Entpackt die heruntergeladenen Dateien; kann sehr lange dauern.
                if (configuration.IsExtractingEnabled) {
                    manager.ExtractTo(
                        configuration.DownloadDirectory,
                        configuration.ExtractingDirectory
                    );
                }
            }
            catch (AggregateException ae)
            {
                exception = ae.InnerException;
            }       
            catch (Exception ex)
            {
                exception = ex;
            }

            // Den neuen Zustand setzten.
            repository.EnterWriteLock(() => {
                // Aufräumen - Wichtig: Handle entfernen.
                manager.Status -= OnStatusUpdate;
                downloads.Remove(download.Name);

                // Zustand setzen.
                if (exception == null) {
                    SetState(GroupState.Completed, download);
                } else if(exception is OperationCanceledException) {
                    SetState(GroupState.Canceled, download);
                } else {
                    SetState(GroupState.Failed, download, exception);
                }
            });
        }

        /// <summary>
        /// Ein Download-Manager hat neue Informationen.
        /// </summary>
        /// <param name="name">Name des Downloads.</param>
        /// <param name="lastException">Optional: Aufgetretene Fehlermeldung.</param>
        /// <param name="newStatus">Der neue Unterstatus.</param>
        private void OnStatusUpdate(string name, Exception lastException, GroupMeta newStatus)
        {
            repository.EnterWriteLock(() => 
            {
                var downloadToChange = repository.Find(name);

                if(downloadToChange.State == GroupState.Running) {
                    SetState(GroupState.Running, downloadToChange, lastException, newStatus);  
                }
            });
        }

        #region Public

        /// <summary>
        /// Erstellt einen neuen Download mit dem angegebenen Inhalt.
        /// </summary>
        /// <param name="name">Der Namen des Downloads.</param>
        /// <param name="files">Links die heruntergeladen werden sollen.</param>
        /// <param name="autostart">Gibt an ob das heruntergeladen automatisch gestartet wird.</param>
        /// 
        /// <exception cref="ArgumentNullException"></exception>
        /// 
        public string Create(string name, string[] files, bool autostart, string password = null, bool skipIfContains = false)
        {
            if(string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            if(files == null) {
                throw new ArgumentNullException(nameof(name));
            }

            DownloadRecord download = null;

            repository.EnterWriteLock(() =>
            {
                var nameCount = 1;
                var nameDownload = PathHelper.Sanitize(name);
                
                if(skipIfContains && repository.Contains(nameDownload)) {
                    return;
                }

                // Neuen Namen finden wenn der aktuelle bereits vergeben ist.
                while (repository.Contains(nameDownload)) {
                    nameDownload = $"{name} {++nameCount}";
                }

                download = new ()
                {
                    Name = nameDownload,
                    State = GroupState.Idle,
                    Files = files,
                    Password = password,
                };

                WriteFileToCache(nameDownload, files);

                bool isSuccessfullyAdded = repository.Add(download);         

                if( ! isSuccessfullyAdded) {
                    throw new InvalidOperationException();
                }

                if(autostart) Start(download.Name);
            });

            return download?.Name;
        }
        
        private void WriteFileToCache(string downloadName, string[] files)
        {
            try {
                // Bei einem Neustart der Anwendung gehen die hinzugefügten Links nicht verloren.
                string cacheFile = Path.Combine(configuration.DownloadDirectory, downloadName + ".txt");

                if( ! File.Exists(cacheFile)) {
                    File.WriteAllLines(cacheFile, files);
                }
            }
            catch (Exception exception)
            {
                exception = new CacheException(exception);

                NotificationRecord clientNotification = new()
                {
                    ErrorType = exception.GetType().ToString(),
                    MessageLog = exception.StackTrace
                };

                logger.LogError(exception, "File could not be cached.");
                broadcaster.Add(CommandFromServer.Error, WebSocketPackage.PARAMETER_UNSET, clientNotification);
            }
        }
        /// <summary>
        /// Startet den angegebenen Download.
        /// </summary>
        /// <param name="name">Der Namen des Downloads.</param>
        /// 
        /// <exception cref="KeyNotFoundException">Die Gruppe wurde nicht gefunden.</exception>
        /// <exception cref="CommandNotAllowedException">Die Gruppe erlaubt den Zustandswechsel nicht.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// 
        public void Start(string name)
        {
            if(string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentNullException(nameof(name));
            }


            repository.EnterWriteLock(() =>
            {
                DownloadRecord download = repository.Find(name);
                
                bool commandValid = download.State == GroupState.Canceled || 
                                    download.State == GroupState.Failed ||
                                    download.State == GroupState.Idle|| 
                                    download.State == GroupState.Completed;

                if( ! commandValid) {
                    throw new CommandNotAllowedException();
                }

                // Aufgabe zur Warteschlange hinzufügen.
                queue.Add(download.Name);

                // Wartenden Threads signalisieren, dass eine Änderung stattgefunden hat.
                queueChangedEvent.Set();

                SetState(GroupState.Queued, download);
            });
        }

        /// <summary>
        /// Stoppt den angegebenen Download.
        /// </summary>
        /// 
        /// <exception cref="KeyNotFoundException">Die Gruppe wurde nicht gefunden.</exception>
        /// <exception cref="CommandNotAllowedException">Die Gruppe erlaubt den Zustandswechsel nicht.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// 
        public void Stop(string name)
        {
            if(string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentNullException(nameof(name));
            }


            repository.EnterWriteLock(() =>
            {
                DownloadRecord download = repository.Find(name);

                bool commandValid = download.State == GroupState.Queued || 
                                    download.State == GroupState.Running;
                                              
                if( ! commandValid) {
                    throw new CommandNotAllowedException();
                }

                bool isRunning = download.State == GroupState.Running;

                if(isRunning)
                {
                    // Zustand davor aktualisieren.
                    SetState(GroupState.Stopping, download);

                    // Abbruchsanforderung senden.
                    downloads[download.Name].Cancel();
                }
                else
                {
                    // Vorgang aus der Warteschlange entfernen.
                    bool isSuccessfully = queue.Remove(name);

                    if( ! isSuccessfully) {
                        throw new StateMaschineException();
                    }

                    // Zustand _danach_ aktualisieren.
                    SetState(GroupState.Idle, download);
                }
            });
        }
        
        public void Delete(string name)
        {
            repository.EnterWriteLock(() =>
            {
                DownloadRecord download = repository.Find(name);

                bool commandValid = download.State == GroupState.Completed || 
                                    download.State == GroupState.Canceled ||
                                    download.State == GroupState.Failed ||
                                    download.State == GroupState.Idle;
                                        
                if( ! commandValid) {
                    throw new CommandNotAllowedException();
                }

                SetState(GroupState.Deleting, download);

                Task.Run(() => {
                    string folderPath = Path.Combine(configuration.DownloadDirectory, download.Name);
                    string filePath = folderPath + ".txt";

                    // Heruntergeladene Dateien löschen.
                    if (Directory.Exists(folderPath)) {
                        Directory.Delete(folderPath, true);
                    }
                    // Zugeordnete Datei löschen.
                    if (File.Exists(filePath)) {
                        File.Delete(filePath);
                    }
                }).ContinueWith((t) => {
                    repository.EnterWriteLock(() =>
                    {
                        if(t.Exception != null) {
                            SetState(GroupState.Failed, download, t.Exception);

                            NotificationRecord clientNotification = new()
                            {
                                ErrorType = t.Exception.GetType().ToString(),
                                MessageLog = t.Exception.StackTrace
                            };

                            logger.LogError(t.Exception, "Deleting a download failed.");
                            broadcaster.Add(CommandFromServer.Error, WebSocketPackage.PARAMETER_UNSET, clientNotification);
                        }
                        else {
                            bool removedSuccessfully = repository.Remove(download);

                            if( ! removedSuccessfully) {
                                throw new StateMaschineException();
                            } 
                        }
                    });
                 }); 
            });
        }

        /// <summary>
        /// Startet alle Downloads.
        /// </summary>
        public void StartAll()
        {
            repository.EnterWriteLock(() =>
            {
                var downloads = repository.AsEnumerable().Where(download => download.State == GroupState.Canceled ||
                                                                            download.State == GroupState.Failed ||
                                                                            download.State == GroupState.Idle);

                foreach(DownloadRecord downloadToStart in downloads.ToArray())
                {
                    Start(downloadToStart.Name);
                }
            });
        }

        /// <summary>
        /// Stoppt alle laufenden Downloads.
        /// </summary>
        public void StopAll()
        {
            repository.EnterWriteLock(() =>
            {
                var downloads = repository.AsEnumerable().Where(download => download.State == GroupState.Running ||
                                                                            download.State == GroupState.Queued);

                foreach(DownloadRecord downloadToStart in downloads.ToArray())
                {
                    Stop(downloadToStart.Name);
                }
            });
        }

        #endregion

        /// <summary>
        /// Ändert den Zustand des angegebenen Download.
        /// </summary>
        /// <param name="state">Der neue Zustand des Downloads.</param>
        /// <param name="download">Der betroffene Download.</param>
        /// <param name="exception">Eine optionale Fehlermeldung.</param>
        private DownloadRecord SetState(GroupState state, DownloadRecord download, Exception exception = null, GroupMeta meta = 0)
        {
            DownloadRecord updatedDownload = null;

            switch(state)
            {
                case GroupState.Canceled:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Canceled,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Completed:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Completed,
                        Meta              = null,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Deleting:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Deleting,
                        Meta              = null,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Failed:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Failed,
                        Meta              = null,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = exception.GetType().ToString(),
                    };

                    logger.LogError(exception, "Download '{0}' failed.", download.Name);

                    break;
                }
                case GroupState.Idle:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Idle,
                        Meta              = null,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Queued:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Queued,
                        Meta              = null,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Running:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Running,
                        Meta              = meta,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Stopping:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Stopping,
                        Meta              = null,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = null,
                    };

                    break;
                }
                case GroupState.Unsupported:
                {
                    updatedDownload = download with
                    {
                        State             = GroupState.Unsupported,
                        BytesPerSecond    = null,
                        SecondsToComplete = null,
                        GroupPercent      = null,
                        LastException     = exception.Message.GetType().ToString(),
                    };

                    break;
                }
            }

            repository.Update(updatedDownload);

            return updatedDownload;
        }
    }
}