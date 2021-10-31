using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components.Logging
{
    public class WebViewLegend
    {
        private readonly ILogger logger;

        public WebViewLegend(ILogger logger)
        {
            this.logger = logger;
        }

        public void Fill() {
            logger.LogTrace("Trace");
            logger.LogDebug("Debug");
            logger.LogInformation("Information");
            logger.LogWarning("Warning");
            logger.LogError("Error");
            logger.LogCritical("Critical");
        }
    }
}