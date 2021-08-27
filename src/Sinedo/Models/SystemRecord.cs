using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.SignalR;

namespace Sinedo.Models
{
    public record SystemRecord
    {
        private static SystemRecord systemInfo;

        public string Hostname { get; set; }
        public string Platform { get; set; }
        public string Architecture { get; set; }
        public int Pid { get; set; }
        public string Version { get; set; }

        public static SystemRecord GetSystemInfo() {
            if (systemInfo == null) {
                systemInfo = new SystemRecord()
                {
                    Hostname        = Environment.MachineName,
                    Platform        = Environment.OSVersion.Platform.ToString(),
                    Architecture    = RuntimeInformation.OSArchitecture.ToString(),
                    Pid             = Environment.ProcessId,
                    Version         = Assembly.GetExecutingAssembly().GetName().Version.ToString(3)
                };
            }

            return systemInfo;
        }
    }
}