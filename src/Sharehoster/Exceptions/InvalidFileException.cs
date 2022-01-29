using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharehoster.Interfaces;

namespace Sharehoster.Exceptions
{
    /// <summary>
    /// Die Datei wurde auf dem Hoster nicht gefunden.
    /// </summary>
    public class InvalidFileException : Exception
    {
        private const string MESSAGE = "The file was not found on the hoster.";

        /// <summary>
        /// Hoster wo das Problem aufgetreten ist.
        /// </summary>
        public ISharehoster Sharehoster { get; init; }
        
        public string Request { get; init; }

        public InvalidFileException(ISharehoster sharehoster, string request) : base(MESSAGE) {
            Sharehoster = sharehoster;
            Request = request;
        }
    }
}
