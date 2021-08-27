using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sinedo.Exceptions
{
    public class ClickAndLoadException : Exception
    {
        private const string MESSAGE = "Links could not be added.";

        public ClickAndLoadException(Exception innerException) : base(MESSAGE, innerException) { }
    }
}