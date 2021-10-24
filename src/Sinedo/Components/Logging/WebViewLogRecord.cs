using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sinedo.Components.Logging
{
    public record WebViewLogRecord
    {
        public string Category { get; init; }

        //
        // Zusammenfassung:
        //     Gets the event type of this entry.
        //
        // Rückgabewerte:
        //     The event type that is associated with the entry in the event log.
        public LogLevel LogLevel { get; init; }

        //
        // Zusammenfassung:
        //     Gets the index of this entry in the event log.
        //
        // Rückgabewerte:
        //     The index of this entry in the event log.
        public long Index { get; init; }

        //
        // Zusammenfassung:
        //     Gets the name of the computer on which this entry was generated.
        //
        // Rückgabewerte:
        //     The name of the computer that contains the event log.
        public string MachineName { get; init; }
        //
        // Zusammenfassung:
        //     Gets the localized message associated with this event entry.
        //
        // Rückgabewerte:
        //     The formatted, localized text for the message. This includes associated replacement
        //     strings.
        //
        // Ausnahmen:
        //   T:System.Exception:
        //     The space could not be allocated for one of the insertion strings associated
        //     with the message.
        public string Message { get; init; }
        //
        // Zusammenfassung:
        //     Gets the name of the application that generated this event.
        //
        // Rückgabewerte:
        //     The name registered with the event log as the source of this event.
        public string Source { get; init; }
        //
        // Zusammenfassung:
        //     Gets the local time at which this event was generated.
        //
        // Rückgabewerte:
        //     The local time at which this event was generated.
        public DateTime TimeGenerated { get; init; }
        //
        // Zusammenfassung:
        //     Gets the name of the user who is responsible for this event.
        //
        // Rückgabewerte:
        //     The security identifier (SID) that uniquely identifies a user or group.
        //
        // Ausnahmen:
        //   T:System.SystemException:
        //     Account information could not be obtained for the user's SID.
        public string UserName { get; init; }

        public static WebViewLogRecord CreateFromTemplate(long index, string category, LogLevel logLevel, string message, string source)
        {
            return new WebViewLogRecord()
            {
                Index = index,
                Category = category,
                Source = source,
                LogLevel = logLevel,
                Message = message,
                UserName = Environment.UserName,
                MachineName = Environment.MachineName,
                TimeGenerated = DateTime.UtcNow,
            };
        }
    }
}
