using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sinedo.Exceptions
{
    public class CacheException : Exception
    {
        private const string MESSAGE = "The file could not be written to the cache.";

        public CacheException(Exception innerException) : base(MESSAGE, innerException) { }
    }
}
