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
    public abstract class DownloadSchedulerFunctions : DownloadSchedulerWorker
    {
        protected void CancelDownload(string name)
        {
            throw new NotImplementedException();
        }

        protected void DeleteFolderInBackground(string name)
        {
            logger.LogInformation("Download {downloadName} will be deleted.", name);

            Task.Run(() =>
            {
                string folderPath = Path.Combine(configuration.DownloadDirectory, name);
                string filePath = folderPath + ".txt";

                // Heruntergeladene Dateien löschen.
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
                // Zugeordnete Datei löschen.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            })
            .ContinueWith(Completion, name);
        }

        private async Task Completion(Task task, object state)
        {
            // Name des Downloads.
            string name = (string)state;

            using (await repository.Context.WriterLockAsync())
            {
                if (task.IsCompletedSuccessfully)
                {
                    if (repository.Remove(name) == false)
                    {
                        throw new StateMaschineException();
                    }

                    logger.LogInformation("Download {downloadName} was successfully deleted.", name);
                }
                else
                {
                    // Diese Fehlermeldung wird in der UI übersetzt.
                    Exception exception = new DeletionFailedException(name, task.Exception);
                    SetState(name, DownloadState.Failed, exception);

                    // Fehlermeldung an alle Clients senden.
                    broadcaster.Add(CommandFromServer.Error, NotificationRecord.FromException(exception));

                    logger.LogError(exception, "Download {downloadName} could not be deleted.", name);
                }
            }
        }
    }
}