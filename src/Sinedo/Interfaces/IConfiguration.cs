using System.ComponentModel;

namespace Sinedo.Singleton
{
    public interface IConfiguration : INotifyPropertyChanged
    {
        public bool IsSetupCompleted { get; }
        public bool NeedServerRestart { get; }
        public byte[] PasswordHash { get; }
        public string IPAddress { get; }
        public int Port { get; }
        public bool RedirectToHttps { get; }
        public uint InternetConnectionInMbits { get; }
        public uint ConcurrentDownloads { get; }
        public string DownloadDirectory { get; }
        public bool IsExtractingEnabled { get; }
        public string ExtractingDirectory { get; }
        public string ExternalUrl { get; }
        public string ApplicationName { get; }
    }
}