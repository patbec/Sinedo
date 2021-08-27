using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        private const string MESSAGE = "The user credentials are invalid.";

        public InvalidCredentialsException() : base(MESSAGE)
        {
        }
    }
}
