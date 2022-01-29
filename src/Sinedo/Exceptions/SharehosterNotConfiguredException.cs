using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class SharehosterNotConfiguredException : Exception
    {
        private const string MESSAGE = "The hoster with the name '{0}' is not configured.";

        public string UnconfiguredSharehoster { get; }

        public SharehosterNotConfiguredException(string name) : base(string.Format(MESSAGE, name))
        {
            UnconfiguredSharehoster = name;
        }
    }
}
