using System;

namespace Sinedo.Models
{
    public record NotificationRecord
    {
        /// <summary>
        /// 
        /// </summary>
        public string ErrorType { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public string MessageLog { get; init; }

        public static NotificationRecord FromException(Exception exception)
        {
            return new NotificationRecord()
            {
                ErrorType = exception.GetType().ToString(),
                MessageLog = exception.StackTrace
            };
        }
    }
}
