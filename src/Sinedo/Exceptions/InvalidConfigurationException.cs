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
    public class InvalidConfigurationException : Exception
    {
        private const string MESSAGE = "The configuration file contains errors.";

        public InvalidConfigurationException(Exception innerException) : base(MESSAGE, innerException) { }
    }
}
