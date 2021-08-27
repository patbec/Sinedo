using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sinedo.Components;

namespace Sinedo.Exceptions
{
    public class InvalidFileException : Exception
    {
        private const string MESSAGE = "The file is not found at the hoster.";

        /// <summary>
        /// Datei-Id die nicht erreichbar ist.
        /// </summary>
        /// <value></value>
        public string FileId { get; init; }

        public InvalidFileException(string fileId) : base(MESSAGE) {
            FileId = fileId;
        }
    }
}
