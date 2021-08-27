using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class InvalidResponseException : Exception
    {
        private const string MESSAGE = "The response from the service could not be processed.";

        public InvalidResponseException(Exception innerException) : base(MESSAGE, innerException) { }
    }
}
