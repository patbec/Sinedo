using System;

namespace Sinedo.Exceptions
{
    [Obsolete("Function is removed from source")]
    public class PathException : Exception
    {
        private const string MESSAGE = "The specified path is invalid.";

        public PathException(Exception innerException) : base(MESSAGE, innerException) { }
    }
}
