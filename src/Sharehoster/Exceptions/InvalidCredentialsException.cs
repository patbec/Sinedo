using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharehoster.Interfaces;

namespace Sharehoster.Exceptions
{
    /// <summary>
    /// Die Anmeldedaten des Benutzers sind ungültig.
    /// </summary>
    public class InvalidCredentialsException : Exception
    {
        private const string MESSAGE = "The user credentials are invalid.";

        /// <summary>
        /// Hoster wo das Problem aufgetreten ist.
        /// </summary>
        public ISharehoster Sharehoster { get; init; }

        public InvalidCredentialsException(ISharehoster sharehoster) : base(MESSAGE) {
            Sharehoster = sharehoster;
        }
    }
}
