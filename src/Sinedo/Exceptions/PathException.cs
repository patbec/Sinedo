using System;

namespace Sinedo.Exceptions
{
    public class PathException : Exception
    {
        private const string MESSAGE = "The specified path is invalid.";

        public PathException(Exception innerException) : base(MESSAGE, innerException) { }
    }
}
