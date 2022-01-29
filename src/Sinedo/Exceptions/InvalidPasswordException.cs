using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharehoster.Interfaces;

namespace Sinedo.Exceptions
{
    /// <summary>
    /// Die Anmeldedaten des Benutzers sind ungültig.
    /// </summary>
    public class InvalidPasswordException : Exception
    {
        private const string MESSAGE = "The user credentials are invalid.";

        public InvalidPasswordException() : base(MESSAGE) { }
    }
}
