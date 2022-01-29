using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharehoster.Interfaces;

namespace Sharehoster.Exceptions
{
    /// <summary>
    /// Die Antwort des Dienstes konnte nicht verarbeitet werden.
    /// </summary>
    public class InvalidResponseException : Exception
    {
        private const string MESSAGE = "The response from the service could not be processed.";

        /// <summary>
        /// Hoster wo das Problem aufgetreten ist.
        /// </summary>
        public ISharehoster Sharehoster { get; init; }

        public InvalidResponseException(ISharehoster sharehoster, Exception innerException) : base(MESSAGE, innerException) {
            Sharehoster = sharehoster;
        }
    }
}
