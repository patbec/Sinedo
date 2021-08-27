using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Readers;
using Sinedo.Components.Common;
using Sinedo.Components.Sharehoster;
using Sinedo.Exceptions;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Components
{
    public class DownloadManager {
        private readonly CancellationTokenSource cancellationTokenSource = new ();
        private readonly ISharehoster sharehoster = new Rapidgator();

        private readonly NetworkCredential credential;

        private readonly string downloadName;
        private readonly string[] downloadFiles;
        private readonly string downloadPassword;

        private MonitoringHelper monitoring;
        
        public StatusEventHandler Status;

        public delegate void StatusEventHandler(string name, Exception lastException, GroupMeta newStatus);


        public MonitoringHelper Monitoring => monitoring;

        /// <summary>
        /// Erstellt einen neuen Download-Manager.
        /// </summary>
        /// <param name="credential">Anmeldedaten für den Dienst.</param>
        /// <param name="folderPath">Speicherort für die heruntergeladenen Dateien.</param>
        /// <param name="downloadRecord">Ordnername, Password und die Dateien zum Herunterladen.</param>
        public DownloadManager(string username, string password, DownloadRecord download) {
            credential = new NetworkCredential(username, password);

            downloadName = download.Name;
            downloadFiles = download.Files;
            downloadPassword = download.Password;
        }

        public long Update()
        {
            if(monitoring == null) {
                return 0;
            }

            return monitoring.Update();
        }

        public void Cancel() {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Lädt die Dateien herunter.
        /// </summary>
        public void DownloadTo(string targetFolder)
        {
            if(string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password)) {
                throw new MissingCredentialsException();
            }


            var token = sharehoster.GetAccessToken(credential.UserName, credential.Password, cancellationTokenSource.Token).GetAwaiter().GetResult();

            // Neues Verzeichnis für den Download erstellen. 
            var folderPath = CreateAndGetFolder(targetFolder, downloadName);

            // Dateigrößen abrufen sowie überprüfen ob alle Dateien erreichbar sind.
            var fileInfos = GetFileInfos(downloadFiles, token, cancellationTokenSource.Token);

            // Verhindert Fehler wenn Dateien später nicht erstellt werden können.
            var handles = new Dictionary<string, FileStream>();


            try
            {
                // Aktueller Fortschritt.
                long sizeCurrent = 0;
                long sizeTotal = 0;

                // Nach bereits heruntergeladene Dateien suchen.
                foreach (SharehosterFile file in fileInfos)
                {
                    var fileStream = new FileStream(GetSafePath(folderPath, file.Name),
                                                    FileMode.OpenOrCreate,
                                                    FileAccess.ReadWrite,
                                                    FileShare.None);

                    // Aktuellen Fortschritt berechnen, bereits heruntergeladenen Bytes zählen.
                    sizeCurrent += fileStream.Length;
                    sizeTotal += file.Size;

                    handles.Add(file.Uid, fileStream);
                }

                // Prüfen ob alle Dateien bereits heruntergeladen wurden.
                if(sizeCurrent == sizeTotal) {
                    return;
                }


                // Fortschrittsanbieter für das Monitoring erstellen.
                // Kann Null sein wenn keine Fortschrittsanzeige benötigt wird.
                monitoring = new MonitoringHelper(sizeTotal, sizeCurrent);

                // Bei Internetproblemen 4 erneute Versuche, 10 Sekunden warten. 
                RetryIfConnectionLost(4, 10, () =>
                {
                    Status(downloadName, null, GroupMeta.Download);

                    // Beginne Dateien herunterzuladen.
                    foreach (SharehosterFile file in fileInfos)
                    {
                        // Geöffneten Handle abrufen.
                        var fileStream = handles[file.Uid];

                        // Prüfen ob die Datei übersprungen werden soll.
                        if(fileStream.Length == file.Size) {
                            continue;
                        }
                        fileStream.Position = fileStream.Length;
                        
                        // Herunterladen.
                        sharehoster.DownloadFileAsync(file,
                                                      fileStream,
                                                      monitoring.Add,
                                                      token,
                                                      cancellationTokenSource.Token).GetAwaiter().GetResult();
                        
                        fileStream.Flush();
                    }
                }, (exception) =>
                {
                    // Bei einer Ausnahme im Retry, Fehlermeldung dem Download zuweisen.
                    Status(downloadName, exception, GroupMeta.Retry);
                }, cancellationTokenSource.Token);
            }
            finally
            {
                monitoring = null;
                
                foreach (var fileStream in handles.Values)
                {
                    fileStream.Close();
                }
            }
        }

        /// <summary>
        /// Entpackt die heruntergeladenen Dateien.
        /// </summary>
        public void ExtractTo(string sourceFolder, string targetFolder)
        {
            List<RarArchive> archives = new ();

            try
            {
                // Neues Verzeichnis zum entpacken erstellen. 
                var downloadPath = CreateAndGetFolder(sourceFolder, downloadName);
                var extractPath = CreateAndGetFolder(targetFolder, downloadName);

                // Startdateien suchen.
                var archivesToExtract = Directory.GetFiles(downloadPath, "*.part1.rar");

                if(archivesToExtract.Length != 0)
                {
                    // Neuen Status an den Scheuduler melden.
                    Status(downloadName, null, GroupMeta.Extract);

                    // Passwort zum entschlüsseln festlegen.
                    ReaderOptions options = new () {
                        Password = downloadPassword
                    };

                    // Startarchive suchen und öffnen.
                    foreach (var file in archivesToExtract)
                    {
                        archives.Add(
                            RarArchive.Open(file, options));
                    }

                    // Fortschrittsanbieter für das Monitoring erstellen.
                    monitoring = new MonitoringHelper(archives.Sum(a => a.TotalSize), 0);

                    // Alle Archive mit den Prefix part1.rar entpacken.
                    foreach (var archive in archives)
                    {
                        // Ordner erstellen und Dateien entpacken.
                        foreach (var entry in archive.Entries)
                        {
                            string fileName = Sanitizer.Sanitize(Path.GetFileName(entry.Key));
                            string fullPath = Path.Combine(extractPath, fileName);

                            // Dateien entpacken.
                            if ( ! entry.IsDirectory)
                            {
                                Stream targetStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                                Stream sourceStream = entry.OpenEntryStream();

                                // Jumbo-Buffer erstellen.
                                byte[] buffer = new byte[65536];

                                // Anzahl der gelesenen Bytes in einer Sequenz.
                                int bytesRead;

                                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    // Buffer in die Datei schreiben.
                                    targetStream.Write(buffer, 0, bytesRead);

                                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                                    // Gelesene Bytes an den Fortschrittsanbieter übermitteln.
                                    monitoring.Add(bytesRead);
                                }

                                targetStream.Flush();
                            }
                        }               
                    }
                }
            }
            finally
            {
                monitoring = null;

                foreach (var item in archives)
                {
                    item.Dispose();
                }
            }
        }

        private static void RetryIfConnectionLost(int count, int delay, Action callback, Action<Exception> errorHandler, CancellationToken cancellationToken) {
            while(count != 0) {
                try {
                    callback();
                    return;
                }
                catch(IOException we)
                {
                    count--;

                    errorHandler(we);
                    if(count == 0) {
                        throw;
                    }
                }

                Task.Delay(delay * 1000, cancellationToken).GetAwaiter().GetResult();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private IEnumerable<SharehosterFile> GetFileInfos(string[] filePaths, string token, CancellationToken cancellationToken)
        {
            SharehosterFile[] files = new SharehosterFile[filePaths.Length];

            for (int i = 0; i < filePaths.Length; i++) {
                string fileId = new Uri(filePaths[i]).Segments[2];
                fileId = fileId.Remove(fileId.Length - 1, 1);
                files[i] = sharehoster.GetFileInfoAsync(fileId, token, cancellationToken).GetAwaiter().GetResult();
            }

            return files;
        }

        /// <summary>
        /// Entfernt ungültige Zeichen aus dem Dateinamen.
        /// </summary>
        private static string GetSafePath(string path, string fileName)
        {
            fileName = Sanitizer.Sanitize(fileName);

            return Path.Combine(path, fileName);
        }

        /// <summary>
        /// Erstellt einen neuen Ordner im Download Verzeichnis des Benutzers.
        /// </summary>
        private static string CreateAndGetFolder(string path, string downloadName)
        {
            #if DEBUG
            string sanitizedName = Sanitizer.Sanitize(downloadName);

            if(sanitizedName != downloadName) {
                throw new ArgumentException("The name was not checked for invalid characters.");
            }
            #endif
            
            string folderPath = Path.Combine(path, downloadName);
            
            return Directory.CreateDirectory(folderPath).FullName;
        }
    }

}