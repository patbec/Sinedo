using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class SharehosterNotSupportedException : Exception
    {
        private const string MESSAGE = "The hoster with the name '{0}' is not present in this version.";

        public string UnsupportedSharehoster { get; }

        public SharehosterNotSupportedException(string name) : base(string.Format(MESSAGE, name))
        {
            UnsupportedSharehoster = name;
        }
    }
}
