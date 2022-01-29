using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
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
    /// 
    /// </summary>
    public abstract class DownloadSchedulerCreate : DownloadSchedulerBase
    {
        protected DownloadRecord AddDownloadToRepository(string name, string[] files, string password = null)
        {
            name = Sanitizer.Sanitize(name);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (files == null || files.Length == 0)
            {
                throw new ArgumentNullException(nameof(files));
            }

            if (repository.Contains(name))
            {
                ThrowIfDownloadIsAlreadyAdded(name, files, password);
                name = GetUniqueDownloadName(name);
            }

            DownloadRecord download = new()
            {
                Name = name,
                State = DownloadState.Idle,
                Files = files,
                Password = password,
            };

            WriteDownloadToCache(download);

            if (!repository.Add(download))
            {
                throw new StateMaschineException();
            }

            return download;
        }

        private void ThrowIfDownloadIsAlreadyAdded(string name, string[] files, string password = null)
        {
            DownloadRecord existingDownload = repository.Find(name);

            bool isPresent = existingDownload.Files.SequenceEqual(files) &&
                             existingDownload.Password == password;

            if (isPresent)
            {
                Exception exception = new DuplicateDownloadException(name);

                logger.LogWarning(exception, "Download with the name '{downloadName}' is already in the list.", name);
                throw exception;
            }
        }

        private string GetUniqueDownloadName(string name)
        {
            int nameCount = 1;
            string nameDownload;

            do
            {
                nameDownload = $"{name} {++nameCount}";
            } while (repository.Contains(nameDownload));

            return nameDownload;
        }

        private void WriteDownloadToCache(DownloadRecord download)
        {
            try
            {
                // Bei einem Neustart der Anwendung gehen die hinzugefügten Links nicht verloren.
                string filePath = Path.Combine(configuration.DownloadDirectory, download.Name + ".json");

                download.Save(filePath);
            }
            catch (Exception exception)
            {
                throw new CacheException(exception);
            }
        }
    }
}