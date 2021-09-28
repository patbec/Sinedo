using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public class DownloadRepository
    {
        private readonly WebSocketBroadcaster _broadcaster;
        private readonly ILogger<DownloadRepository> _logger;
        private readonly Dictionary<string, DownloadRecord> _repository = new();
        private readonly ReaderWriterLockSlim _context = new(LockRecursionPolicy.SupportsRecursion);

        public DownloadRepository(WebSocketBroadcaster broadcaster, ILogger<DownloadRepository> logger)
        {
            _broadcaster = broadcaster;
            _logger = logger;
        }

        /// <summary>
        /// Sperrt den aktuellen Synchronisierung-Kontext für Lesezugriffe.
        /// </summary>
        public void EnterReadLock(Action action) {
            _context.EnterReadLock();
            try {
                action();
            } finally {
                _context.ExitReadLock();
            }
        }

        /// <summary>
        /// Sperrt den aktuellen Synchronisierung-Kontext für einen Schreibzugriff.
        /// </summary>
        public void EnterWriteLock(Action action) {
            _context.EnterWriteLock();
            try {
                action();
            } finally {
                _context.ExitWriteLock();
            }
        }

        public bool Add(DownloadRecord download)
        {
            if (download is null)
            {
                throw new ArgumentNullException(nameof(download));
            }

            _repository.Add(download.Name, download);
            _broadcaster.Add(CommandFromServer.Added, WebSocketPackage.PARAMETER_UNSET, download);
            _logger.LogDebug("Download with name '{0}' was added.", download.Name);

            return true;
        }

        public bool Remove(DownloadRecord torrent)
        {
            return Remove(torrent.Name);
        }

        public bool Remove(string name)
        {
            bool wasSuccessfullyRemoved = _repository.Remove(name);

            if( ! wasSuccessfullyRemoved) {
                return false;
            }

            _broadcaster.Add(CommandFromServer.Removed, WebSocketPackage.PARAMETER_UNSET, name);
            _logger.LogDebug("Download with name '{0}' was removed.", name);

            return true;
        }
        public bool Contains(string name)
        {
            return _repository.ContainsKey(name);
        }

        public bool Update(DownloadRecord download)
        {
            bool contains = _repository.ContainsKey(download.Name);

            if ( ! contains)
            {
                return false;
            }

            _repository[download.Name] = download;

            _broadcaster.Add(CommandFromServer.Changed, WebSocketPackage.PARAMETER_UNSET, download);
            _logger.LogDebug("Download with name '{0}' was updated.", download.Name);

            return true;
        }

        public DownloadRecord Find(string name)
        {
            if( ! _repository.TryGetValue(name, out var item)){
                throw new KeyNotFoundException($"Download '{name}' not found in repository.");
            }

            return item;
        }

        public DownloadRecord FindOrDefault(string name)
        {
            if( ! _repository.TryGetValue(name, out var item)){
                return null;
            }

            return item;
        }
        public IEnumerable<DownloadRecord> AsEnumerable()
        {
            return _repository.Values.AsEnumerable();
        }
    }
}