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
    public class WebViewLogger : ILogger
    {
        private const int MAX_LOG_ITEMS = 5000;

        private readonly Queue<WebViewLogRecord> _items = new();

        private long _itemsCounter = 0;
        private LogLevel _statusLevel = 0;

        /// <summary>
        /// Anzeigename des Loggers.
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// Namensraum des Loggers.
        /// </summary>
        public string ComponentNamespace { get; }

        /// <summary>
        /// Gibt an ob die Komponente auf einen Internen Namensraum zeigt.
        /// </summary>
        public bool Internal { get; }

        /// <summary>
        /// Erstellt einen neuen WebViewLogger.
        /// </summary>
        /// <param name="compontentName">Anzeigename des Loggers.</param>
        /// <exception cref="ArgumentNullException"/>
        public WebViewLogger(string componentNamespace)
        {
            ComponentNamespace = componentNamespace ?? throw new ArgumentNullException(nameof(componentNamespace));
            ComponentName = componentNamespace.Split('.').LastOrDefault();

            Internal = componentNamespace.StartsWith(nameof(Microsoft));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return default;
        }

        /// <summary>
        /// Es sind nur Fehler und Warnungen aktiviert.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Information || 
                   logLevel == LogLevel.Error ||
                   logLevel == LogLevel.Critical ||
                   logLevel == LogLevel.Warning;
        }

        /// <summary>
        /// Logische Implementierung von Microsoft übernommen:
        /// https://github.com/aspnet/Logging/blob/master/src/Microsoft.Extensions.Logging.Debug/DebugLogger.cs
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (IsEnabled(logLevel))
            {
                var eventCategory = ComponentName;
                var eventLogLevel = logLevel;
                var eventMessage = formatter(state, exception);
                var eventSource = nameof(Sinedo);

                if (string.IsNullOrEmpty(eventMessage))
                {
                    return;
                }

                if (exception != null)
                {
                    eventMessage += Environment.NewLine + Environment.NewLine + exception.ToString();
                }

                var eventIndex = Interlocked.Increment(ref _itemsCounter);

                var eventItem = WebViewLogRecord.CreateFromTemplate(eventIndex,
                                                                    eventCategory,
                                                                    eventLogLevel,
                                                                    eventMessage,
                                                                    eventSource);
        
                lock(_items)
                {
                    // Update current error level.
                    // Used in FrontEnd and show the symbol: Warn or Critical Icon
                    if (_statusLevel < eventLogLevel)
                        _statusLevel = eventLogLevel;

                    // Element limit.
                    if (_items.Count > MAX_LOG_ITEMS)
                        _items.Dequeue();

                    _items.Enqueue(eventItem);
                }
            }
        }

        public WebViewLogRecord[] GetLogItems()
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }

        public LogLevel GetStatusLevel()
        {
            lock (_items)
            {
                return _statusLevel;
            }
        }

        public int GetLogItemsCount()
        {
            lock (_items)
            {
                return _items.Count;
            }
        }

        public void Clear()
        {
            lock (_items)
            {
                _statusLevel = 0;
                _items.Clear();

                // Dont Reset Index
            }
        }
    }
}