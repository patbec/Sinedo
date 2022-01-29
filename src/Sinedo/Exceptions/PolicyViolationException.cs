using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Sinedo.Components;
using Sinedo.Flags;

namespace Sinedo.Exceptions
{
    public class PolicyViolationException : Exception
    {
        private const string MESSAGE = "An attempt was made to send a packet to a channel that does not have access permission.";

        public Guid ClientUid { get; }

        public CommandFromClient? Command { get; }

        public PolicyViolationException(WebSocketEndpoint webSocketEndpoint, WebSocketPackage webSocketPackage) : base(MESSAGE)
        {
            if(webSocketEndpoint != null) {
                ClientUid = webSocketEndpoint.Uid;
            }

            if(webSocketPackage != null) {
                Command = webSocketPackage.Command;
            }
        }
    }
}
