using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Sinedo.Components.Logging
{
    /// <summary>
    /// Ein Ereignismelder für das WebInterface.
    /// </summary>
    public class WebViewLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, WebViewLogger> _webViewLoggers = new();

        public WebViewLogger[] Loggers
        {
            get => _webViewLoggers.Values.ToArray();
        }

        /// <summary>
        /// Ruft den für diese Instanz vorgegebenen Anbieter ab.
        /// </summary>
        public static WebViewLoggerProvider Default
        {
            get;
        } = new();

        public WebViewLoggerProvider() {
            var legend = new WebViewLegend(CreateLogger("Sinedo.Help.Legend"));
            legend.Fill();
        }

        public ILogger CreateLogger(string componentNamespace)
        {
            return _webViewLoggers.GetOrAdd(componentNamespace, name => new WebViewLogger(name));
        }

        public void Dispose()
        {
            _webViewLoggers.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
