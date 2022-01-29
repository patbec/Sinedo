using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class DeletionFailedException : Exception
    {
        private const string MESSAGE = "Download {0} could not be deleted.";

        public string DownloadName { get; }

        public DeletionFailedException(string name, Exception innerException) : base(string.Format(MESSAGE, name), innerException)
        {
            DownloadName = name;
        }
    }
}
