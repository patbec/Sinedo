using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sinedo.Components;

namespace Sinedo.Exceptions
{
    public class ExceededTrafficException : Exception
    {
        private const string MESSAGE = "Exceeded traffic on the sharehoster.";

        /// <summary>
        /// Datei-Id die nicht erreichbar ist.
        /// </summary>
        /// <value></value>
        public string FileId { get; init; }

        public ExceededTrafficException(string fileId) : base(MESSAGE) {
            FileId = fileId;
        }
    }
}
