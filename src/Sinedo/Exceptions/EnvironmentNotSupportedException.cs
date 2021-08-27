using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Environment;

namespace Sinedo.Exceptions
{
    public class EnvironmentNotSupportedException : Exception
    {
        private const string MESSAGE = "Path {0} is not supported on this platform.";

        public EnvironmentNotSupportedException(SpecialFolder specialFolder) : base(String.Format(MESSAGE, specialFolder), new PlatformNotSupportedException()) { }
    }
}