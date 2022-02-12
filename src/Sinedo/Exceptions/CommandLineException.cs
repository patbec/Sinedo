using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class CommandLineException : Exception
    {
        public CommandLineException(string message) : base(message) { }
    }
}
