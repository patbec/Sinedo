using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Sinedo.Components.Common;
using Sinedo.Components.Sharehoster;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo.Components
{
    public class Downloader {
        private readonly CancellationTokenSource cancellationTokenSource = new ();
        private readonly CancellationToken cancellationToken;
        private readonly string downloadName;
        private readonly string downloadPassword;
        private readonly string[] filesToDownload;

        private DownloadEnviroment env;

        #region Events

        public StatusEventHandler Status;

        public delegate void StatusEventHandler(GroupMeta progress);

        #endregion

        public Monitoring Monitoring => env?.Monitoring;

        /// <summary>
        /// Erstellt einen neuen Download-Manager.
        /// </summary>
        /// <param name="credential">Anmeldedaten für den Dienst.</param>
        /// <param name="folderPath">Speicherort für die heruntergeladenen Dateien.</param>
        /// <param name="downloadRecord">Ordnername, Password und die Dateien zum Herunterladen.</param>
        public Downloader(DownloadRecord download)
        {
            cancellationToken = cancellationTokenSource.Token;

            downloadName = download.Name;
            filesToDownload = download.Files;
            downloadPassword = download.Password;
        }

        public long Update()
        {
            return env?.Monitoring?.Update() ?? 0;
        }

        public void Cancel() {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Lädt die Dateien herunter.
        /// </summary>
        public void DownloadTo(Configuration configuration)
        {
            // Configure Sharehoster
            ISharehoster sharehoster = new Rapidgator (
                new NetworkCredential(configuration.SharehosterUsername, configuration.SharehosterPassword));

            string sanitizedPath = SantanizePath(configuration.DownloadDirectory, downloadName);

            env = new DownloadEnviroment(
                sharehoster,
                sanitizedPath,
                filesToDownload,
                cancellationToken);

            try {
                Status(GroupMeta.CheckStatus);
                env.GetFileInfosFromApi();
                env.MakeFiles();

                if( ! env.IsDownloadCompleted)
                {
                    Status(GroupMeta.Download);
                    // Bei Internetproblemen: 30 Versuche â 30 Sekunden
                    RetryIfConnectionLost(30, 30, () => env.Download());
                }
                
                if( ! configuration.IsExtractingEnabled) {
                    return;
                }

                Status(GroupMeta.Extract);
                env.Extract(configuration.ExtractingDirectory, downloadPassword);
            }
            finally
            {
                env.Dispose();
            }
        }


        private void RetryIfConnectionLost(int count, int delay, Action callback) {
            while(count != 0) {
                try {
                    callback();
                    return;
                }
                catch(IOException)
                {
                    count--;

                    Status(GroupMeta.Retry);
                    if(count == 0) {
                        throw;
                    }

                    // ToDo: Log
                }

                Task.Delay(delay * 1000, cancellationToken).GetAwaiter().GetResult();
                cancellationToken.ThrowIfCancellationRequested();
                Status(GroupMeta.Download);
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