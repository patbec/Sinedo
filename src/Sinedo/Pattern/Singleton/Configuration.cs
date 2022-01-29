using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sinedo.Components;
using Sinedo.Exceptions;

namespace Sinedo.Singleton
{
    public class Configuration : IConfiguration
    {
        private readonly ConfigurationData _data;
        private readonly FileStream _fileStream;

        private static IConfiguration _current;
        public static IConfiguration Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new Configuration(
                    Path.Combine(AppDirectories.ConfigDirectory, "config.json"));
                }

                return _current;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSetupCompleted
        {
            get => _data.PasswordHash != null;
        }

        /// <summary>
        /// Einige geänderte Einstellungen erfordern einen Serverneustart.
        /// </summary>
        /// <value></value>
        public bool NeedServerRestart
        {
            get;
            private set;
        }

        /// <summary>
        /// Das mit SHA512 gehashte Anmeldepasswort.
        /// </summary>
        public byte[] PasswordHash
        {
            get => _data.PasswordHash;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(PasswordHash), "The specified login password is null.");
                }

                lock (this)
                {
                    _data.PasswordHash = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// IP-Adresse auf der das Webinterface erreichbar ist.
        /// </summary>
        public string IPAddress
        {
            get => _data.IPAddress;
            set
            {
                lock (this)
                {
                    _data.IPAddress = value;
                    Save();
                    NeedServerRestart = true;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Port auf dem das Webinterface erreichbar ist.
        /// </summary>
        public int Port
        {
            get => _data.Port;
            set
            {
                lock (this)
                {
                    _data.Port = value;
                    Save();
                    NeedServerRestart = true;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gibt an ob automatisch auf https umgeleitet werden soll.
        /// </summary>
        public bool RedirectToHttps
        {
            get => _data.RedirectToHttps;
            set
            {
                lock (this)
                {
                    _data.RedirectToHttps = value;
                    Save();
                    NeedServerRestart = true;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Geschwindigkeit der Internetverbindung in Mbits.
        /// </summary>
        public uint InternetConnectionInMbits
        {
            get => _data.InternetConnectionInMbits;
            set
            {
                lock (this)
                {
                    _data.InternetConnectionInMbits = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Anzahl der gleichzeitigen Downloads.
        /// </summary>
        public uint ConcurrentDownloads
        {
            get => _data.ConcurrentDownloads;
            set
            {
                lock (this)
                {
                    _data.ConcurrentDownloads = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Zielordner für die heruntergeladenen Dateien.
        /// </summary>
        public string DownloadDirectory
        {
            get => _data.DownloadDirectory;
            set
            {
                lock (this)
                {
                    _data.DownloadDirectory = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gibt an ob heruntergeladene Dateien entpackt werden sollen.
        /// </summary>
        public bool IsExtractingEnabled
        {
            get => _data.IsExtractingEnabled;
            set
            {
                lock (this)
                {
                    _data.IsExtractingEnabled = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Zielordner für die entpackten Dateien.
        /// </summary>
        public string ExtractingDirectory
        {
            get => _data.ExtractingDirectory;
            set
            {
                lock (this)
                {
                    _data.ExtractingDirectory = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Unter dieser Url ist der Server erreichbar.
        /// </summary>
        public string ExternalUrl
        {
            get => _data.ExternalUrl;
            set
            {
                lock (this)
                {
                    _data.ExternalUrl = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        ///  Diesen Namen sendet der Server im Netzwerk.
        /// </summary>
        public string ApplicationName
        {
            get => _data.ApplicationName;
            set
            {
                lock (this)
                {
                    _data.ApplicationName = value;
                    Save();
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Load or create a configuration file.
        /// </summary>
        /// 
        /// <exception cref="IOException"/>
        /// <exception cref="SecurityException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        public Configuration(string filePath)
        {
            Console.WriteLine($"Configuration file path for user {Environment.UserName}: " + filePath);
            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            if (_fileStream.Length == 0)
            {
                Console.WriteLine("Configuration file does not exist or is empty. Create new default configuration.");
                _data = ConfigurationData.GetDefault();
                Save();
            }
            else
            {
                try
                {
                    _data = Load();
                }
                catch (IOException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InvalidConfigurationException(ex);
                }
            }
        }

        /// <summary>
        /// Load data from filesystem.
        /// </summary>
        /// 
        /// <exception cref="JsonException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// 
        /// <returns></returns>
        private ConfigurationData Load()
        {
            byte[] buffer = new byte[_fileStream.Length];
            _fileStream.Read(buffer, 0, buffer.Length);

            ConfigurationData result = JsonSerializer.Deserialize<ConfigurationData>(buffer);
            Console.WriteLine($"Configuration loaded. ({buffer.Length} Bytes)");

            return result;
        }

        /// <summary>
        /// Write data to filesystem.
        /// </summary>
        /// 
        /// <exception cref="IOException"/>
        private void Save()
        {
            string json = JsonSerializer.Serialize(_data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            _fileStream.SetLength(0);
            _fileStream.Write(buffer, 0, buffer.Length);
            _fileStream.Flush();

            Console.WriteLine($"Configuration saved. ({buffer.Length} Bytes)");
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static byte[] ComputeHash(string passwordToHash)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(passwordToHash);

            HashAlgorithm sha = SHA512.Create();
            return sha.ComputeHash(passwordBytes);
        }
    }

    /// <summary>
    /// Einstellungsdatei mit geschützten Anmeldedaten.
    /// </summary>
    public class ConfigurationData
    {
        private byte[] _passwordHash;
        private string _ipAddress;
        private int _port;
        private bool _redirectToHttps;
        private uint _internetConnectionInMbits;
        private uint _concurrentDownloads;
        private string _downloadDirectory;
        private bool _isExtractingEnabled;
        private string _extractingDirectory;
        private string _externalUrl;
        private string _applicationName;

        public static ConfigurationData GetDefault()
        {
            return new ConfigurationData()
            {
                PasswordHash = null,
                IPAddress = "0.0.0.0",
                Port = 2222,
                InternetConnectionInMbits = 50,
                ConcurrentDownloads = 2,
                DownloadDirectory = Path.Combine(AppDirectories.HomeDirectory, "Downloads"),
                ExtractingDirectory = Path.Combine(AppDirectories.HomeDirectory, "Sinedo"),
                IsExtractingEnabled = false,
                ExternalUrl = null,
                ApplicationName = null
            };
        }

        /// <summary>
        /// Das mit SHA512 gehashte Anmeldepasswort.
        /// </summary>
        public byte[] PasswordHash
        {
            get => _passwordHash;
            set
            {
                if (value != null && value.Length != 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(PasswordHash), value.Length, "The specified login password is invalid. The 512 SHA hashed login password must be 64 bytes long.");
                }

                _passwordHash = value;
            }
        }

        /// <summary>
        /// IP-Adresse auf der das Webinterface erreichbar ist.
        /// </summary>
        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(IPAddress), "The specified IP address is null.");
                }

                if (!System.Net.IPAddress.TryParse(value, out var ip))
                {
                    throw new ArgumentOutOfRangeException(nameof(IPAddress), value, "The IP address is not valid.");
                }

                _ipAddress = ip.ToString();
            }
        }

        /// <summary>
        /// Port auf dem das Webinterface erreichbar ist.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Port), value, "Must be non-negative.");
                }

                _port = value;
            }
        }

        /// <summary>
        /// Gibt an ob automatisch auf https umgeleitet werden soll.
        /// </summary>
        [Obsolete("Use Reverse Proxy")]
        public bool RedirectToHttps
        {
            get => _redirectToHttps;
            set
            {
                _redirectToHttps = value;
            }
        }

        /// <summary>
        /// Geschwindigkeit der Internetverbindung in Mbits.
        /// </summary>
        public uint InternetConnectionInMbits
        {
            get => _internetConnectionInMbits;
            set
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(InternetConnectionInMbits), value, "The value must not be 0.");
                }

                _internetConnectionInMbits = value;
            }
        }

        /// <summary>
        /// Anzahl der gleichzeitigen Downloads.
        /// </summary>
        public uint ConcurrentDownloads
        {
            get => _concurrentDownloads;
            set
            {
                if (value == 0 || value > 500)
                {
                    throw new ArgumentOutOfRangeException(nameof(ConcurrentDownloads), value, "The value must be within 1 to 500.");
                }

                _concurrentDownloads = value;
            }
        }

        /// <summary>
        /// Zielordner für die heruntergeladenen Dateien.
        /// </summary>
        public string DownloadDirectory
        {
            get => _downloadDirectory;
            set
            {
                _downloadDirectory = value ?? "";
            }
        }

        /// <summary>
        /// Gibt an ob heruntergeladene Dateien entpackt werden sollen.
        /// </summary>
        public bool IsExtractingEnabled
        {
            get => _isExtractingEnabled;
            set
            {
                _isExtractingEnabled = value;
            }
        }

        /// <summary>
        /// Zielordner für die entpackten Dateien.
        /// </summary>
        public string ExtractingDirectory
        {
            get => _extractingDirectory;
            set
            {
                _extractingDirectory = value ?? "";
            }
        }

        /// <summary>
        ///  Unter dieser Url ist der Server erreichbar.
        /// </summary>
        public string ExternalUrl
        {
            get => _externalUrl;
            set
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out Uri externalUrl))
                {
                    throw new ArgumentException("The specified external url is invalid.", new UriFormatException("The url must look like this: https://sinedo.com"));
                }

                _externalUrl = externalUrl.AbsolutePath;
            }
        }

        /// <summary>
        /// Diesen Namen sendet der Server im Netzwerk.
        /// </summary>
        public string ApplicationName
        {
            get => _applicationName;
            set
            {
                if (value != null)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("The application name '{name}' is not allowed.", value);
                    }

                    value = value.Trim();

                    if (value.Length < 255)
                    {
                        throw new ArgumentOutOfRangeException(nameof(ApplicationName), value.Length, "The application name must not be longer than 255 characters.");
                    }
                }

                _applicationName = value;
            }
        }
    }
}