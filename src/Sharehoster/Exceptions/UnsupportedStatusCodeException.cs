using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharehoster.Interfaces;

namespace Sharehoster.Exceptions
{
    /// <summary>
    /// Der Hoster hat einen nicht unterstützten Statuscode zurückgegeben.
    /// </summary>
    public class UnsupportedStatusCodeException : Exception
    {
        private const string MESSAGE = "The hoster has returned an unsupported status code.";

        public string Request { get; init; }    
        public int StatusCode { get; }
        public string StatusMessage { get; }

        public UnsupportedStatusCodeException(string request, int statusCode, string statusMessage) : base(MESSAGE) {
            Request = request;
            StatusCode = statusCode;
            StatusMessage = statusMessage;
        }
    }
}
