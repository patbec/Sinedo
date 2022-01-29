using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sinedo.Models;

namespace Sinedo.Exceptions
{
    public class DuplicateDownloadException : Exception
    {
        private const string MESSAGE = "The download is already in the list.";

        public string DownloadName { get; init; }

        public DuplicateDownloadException(string downloadName) : base(MESSAGE)
        {
            DownloadName = downloadName;
        }
    }
}
