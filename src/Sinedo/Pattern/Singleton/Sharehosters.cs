
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sharehoster;
using Sharehoster.Interfaces;
using Sinedo.Exceptions;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    /// <summary>
    /// 
    /// </summary>
    public class Sharehosters
    {
        private readonly Dictionary<string, ISharehoster> _sharehosterList = new();

        private readonly ILogger<Sharehosters> logger;
        private readonly Configuration configuration;

        public Sharehosters(ILogger<Sharehosters> logger, Configuration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public SharehosterFile GetFileInfo(Uri fileUri, CancellationToken cancellationToken = default)
        {
            ISharehoster selectedSharehoster;

            lock (this)
            {
                IEnumerable<ISharehoster> sharehosters = from sharehoster in _sharehosterList.Values
                                                         where sharehoster.IsSupported(fileUri)
                                                         select sharehoster;

                selectedSharehoster = sharehosters.FirstOrDefault();

                if (selectedSharehoster == null)
                {
                    return null;
                }
            }

            return selectedSharehoster.GetFileInfo(fileUri, cancellationToken);
        }

        public Stream GetFileStream(SharehosterFile file, long startPosition, CancellationToken cancellationToken = default)
        {
            ISharehoster selectedSharehoster;

            lock (this)
            {
                IEnumerable<ISharehoster> sharehosters = from sharehoster in _sharehosterList.Values
                                                         where sharehoster.IsSupported(file)
                                                         select sharehoster;

                selectedSharehoster = sharehosters.FirstOrDefault();

                if (selectedSharehoster == null)
                {
                    return null;
                }
            }

            return selectedSharehoster.GetFileStream(file, startPosition, cancellationToken);
        }

        public ISharehoster GetSharehoster(string name)
        {
            lock (this)
            {
                bool sharehosterFound = _sharehosterList.TryGetValue(name, out ISharehoster sharehoster);

                if (sharehosterFound)
                {
                    return sharehoster;
                }

                return null;
            }
        }

        public void AddOrUpdate(string name, string username, string password, string parameter)
        {
            lock (this)
            {
                bool sharehosterFound = _sharehosterList.TryGetValue(name, out ISharehoster sharehoster);

                if (sharehosterFound)
                {
                    sharehoster.Configure(username, password, parameter);
                }
                else
                {
                    ISharehoster sharehosterToAdd = null;

                    switch (name)
                    {
                        case nameof(Rapidgator):
                            {
                                sharehosterToAdd = new Rapidgator();
                                break;
                            }
                        default:
                            {
                                throw new SharehosterNotSupportedException(name);
                            }
                    }

                    sharehosterToAdd.Configure(username, password, parameter);
                    _sharehosterList.Add(name, sharehosterToAdd);
                }
            }
        }

        public void Remove(string name)
        {
            lock (this)
            {
                bool sharehosterRemoved = _sharehosterList.Remove(name, out ISharehoster sharehoster);

                if (!sharehosterRemoved)
                {
                    throw new SharehosterNotConfiguredException(name);
                }
            }
        }

        public object Refresh(string name)
        {
            throw new NotImplementedException();
            // lock(this)
            // {

            // }
        }
    }
}