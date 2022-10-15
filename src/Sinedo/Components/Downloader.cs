using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Sharehoster.Interfaces;
using SharpCompress.Archives.Rar;
using SharpCompress.Readers;
using Sinedo.Components.Common;
using Sinedo.Singleton;

namespace Sinedo.Components
{
    [Obsolete]
    public class DownloaderOld : IDisposable
    {
        private readonly Dictionary<string, FileStream> fileHandles = new();
        private readonly Sharehosters sharehosters;
        private readonly string name;
        private readonly string targetPath;
        private readonly string[] filesToDownload;
        public readonly CancellationTokenSource cancellationTokenSource;


        private Monitoring monitoring;
        public string Name => name;

        /// <summary>
        /// Prüfen ob alle Dateien bereits heruntergeladen wurden.
        /// </summary>
        public bool IsDownloadCompleted
        {
            get => bytesDownloaded == bytesTotal;
        }
        public Monitoring Monitoring
        {
            get => monitoring;
        }

        private long bytesDownloaded, bytesTotal = 0;

        public DownloaderOld(Sharehosters sharehosters, string name, string targetPath, string[] filesToDownload)
        {
            this.sharehosters = sharehosters;
            this.name = name;
            this.targetPath = targetPath;
            this.filesToDownload = filesToDownload;
        }

        public List<SharehosterFile> sharehosterFiles;

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }
        public void GetFileInfosFromApi()
        {
            sharehosterFiles = new();

            bytesTotal = 0;

            // Prüfen ob alle Dateien Online sind.
            foreach (string link in filesToDownload)
            {
                if (Uri.TryCreate(link, UriKind.Absolute, out Uri linkUri))
                {
                    var fileInfo = sharehosters.GetFileInfo(linkUri, cancellationTokenSource.Token);

                    bytesTotal += fileInfo.Size;
                    sharehosterFiles.Add(fileInfo);
                }
            }
        }

        public void MakeFiles()
        {
            Directory.CreateDirectory(targetPath);

            bytesDownloaded = 0;

            // Zieldateien erstellen und öffnen.
            foreach (SharehosterFile link in sharehosterFiles)
            {
                string sanitizedPath = Path.Combine(targetPath, Sanitizer.Sanitize(link.Name));

                var fileStream = new FileStream(sanitizedPath,
                                                FileMode.OpenOrCreate,
                                                FileAccess.ReadWrite,
                                                FileShare.None);

                // Aktuellen Fortschritt berechnen, bereits heruntergeladene Bytes zählen.
                bytesDownloaded += fileStream.Length;

                fileHandles.Add(link.Uid, fileStream);
            }
        }

        public void Download()
        {
            // Fortschrittsanbieter für das Monitoring erstellen.
            monitoring = new Monitoring(bytesTotal, bytesDownloaded);

            foreach (var link in sharehosterFiles)
            {
                // Geöffneten Handle abrufen.
                var fileStream = fileHandles[link.Uid];
                var progress = fileStream.Length;

                // Prüfen ob die Datei übersprungen werden soll.
                if (progress == link.Size)
                {
                    continue;
                }
                fileStream.Position = progress;

                using Stream webStream = sharehosters.GetFileStream(
                    link,
                    progress,
                    cancellationTokenSource.Token);

                // Jumbo-Buffer erstellen.
                Span<byte> buffer = new byte[2048];

                // Anzahl der gelesenen Bytes in einer Sequenz.
                int bytesRead;

                // Kopieren bis keine Bytes mehr gelesen wurden.
                while ((bytesRead = webStream.Read(buffer)) > 0)
                {
                    // Buffer in die Datei schreiben.
                    fileStream.Write(buffer[..bytesRead]);

                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Gelesene Bytes an den Fortschrittsanbieter übermitteln.
                    monitoring.Report(bytesRead);
                }
            }

            monitoring = null;
        }

        public void Extract(string extractPath, string password)
        {
            List<RarArchive> archives = new();
            ReaderOptions options = new() { Password = password ?? "", LookForHeader = true };

            foreach (var item in fileHandles.Values)
            {
                var test = RarArchive.Open(fileHandles.Values, options);

                if (test.IsFirstVolume())
                {
                    archives.Add(test);
                }
            }

            // Fortschrittsanbieter für das Monitoring erstellen.
            monitoring = new Monitoring(archives.Sum(a => a.TotalSize), 0);

            foreach (var archive in archives)
            {
                // Ordner erstellen und Dateien entpacken.
                foreach (var entry in archive.Entries)
                {
                    string fileName = Sanitizer.Sanitize(Path.GetFileName(entry.Key));
                    string fullPath = Path.Combine(extractPath, fileName);

                    // Dateien entpacken.
                    if (!entry.IsDirectory)
                    {
                        using Stream targetStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                        using Stream sourceStream = entry.OpenEntryStream();

                        // Jumbo-Buffer erstellen.
                        byte[] buffer = new byte[2048];

                        // Anzahl der gelesenen Bytes in einer Sequenz.
                        int bytesRead;

                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // Buffer in die Datei schreiben.
                            targetStream.Write(buffer, 0, bytesRead);

                            cancellationTokenSource.Token.ThrowIfCancellationRequested();

                            // Gelesene Bytes an den Fortschrittsanbieter übermitteln.
                            monitoring.Report(bytesRead);
                        }

                        targetStream.Flush();
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var item in fileHandles.Values)
            {
                item.Flush();
                item.Dispose();
            }
            monitoring = null;

            GC.SuppressFinalize(this);
        }
    }
}