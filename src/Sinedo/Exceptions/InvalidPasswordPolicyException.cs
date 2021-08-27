using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class InvalidPasswordPolicyException : Exception
    {
        private const string MESSAGE = "The password does not comply with the policy.";

        public InvalidPasswordPolicyException() : base(MESSAGE)
        {
        }
    }
}
