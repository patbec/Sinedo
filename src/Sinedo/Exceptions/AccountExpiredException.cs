using System;
using System.Collections.Generic;
using System.Text;

namespace Sinedo.Exceptions
{
    public class AccountExpiredException : Exception
    {
        private const string MESSAGE = "Access to the account has expired.";

        public AccountExpiredException() : base(MESSAGE)
        {
        }
    }
}
