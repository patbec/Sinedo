using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    [Obsolete("Remove")]
    public class MissingCredentialsException : Exception
    {
        private const string MESSAGE = "The user credentials are missing.";

        public MissingCredentialsException() : base(MESSAGE)
        {
        }
    }
}
