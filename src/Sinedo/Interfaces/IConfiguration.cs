using System.ComponentModel;

namespace Sinedo.Singleton
{
    public interface IConfiguration : INotifyPropertyChanged
    {
        public bool IsSetupCompleted { get; }
        public bool NeedServerRestart { get; }
        public byte[] PasswordHash { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public bool RedirectToHttps { get; set; }
        public uint InternetConnectionInMbits { get; set; }
        public uint ConcurrentDownloads { get; set; }
        public string DownloadDirectory { get; set; }
        public bool IsExtractingEnabled { get; set; }
        public string ExtractingDirectory { get; set; }
        public string ExternalUrl { get; set; }
        public string ApplicationName { get; set; }
    }
}