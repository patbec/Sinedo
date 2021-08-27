using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class StateMaschineException : Exception
    {
        private const string MESSAGE = "The internal state maschine is broken.";

        public StateMaschineException() : base(MESSAGE)
        {
        }
    }
}
