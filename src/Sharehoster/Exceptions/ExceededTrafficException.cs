using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharehoster.Interfaces;

namespace Sharehoster.Exceptions
{
    /// <summary>
    /// Überschreitung des Datenverkehrs auf dem Sharehoster.
    /// </summary>
    public class ExceededTrafficException : Exception
    {
        private const string MESSAGE = "Exceeded traffic on the sharehoster.";

        /// <summary>
        /// Hoster wo das Problem aufgetreten ist.
        /// </summary>
        public ISharehoster Sharehoster { get; init; }

        public ExceededTrafficException(ISharehoster sharehoster) : base(MESSAGE) {
            Sharehoster = sharehoster;
        }
    }
}
