using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;
using Sinedo.Exceptions;

namespace Sinedo.Components
{
    public static class AppDirectories
    {
        /// <summary>
        /// Application directory for configuration data.
        /// Here the settings of the application are stored, the path is resolved as follows:
        /// Linux and macOS to '/home/$USER/.config/sinedo', Windows to 'C:\Users\%USER%\AppData\Roaming\Sinedo'.
        /// </summary>
        /// 
        /// <exception cref="EnvironmentNotSupportedException"/>
        /// 
        /// <returns>The path to the specified special system folder with the application name.</returns>
        public static string ConfigDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? throw new EnvironmentNotSupportedException(Environment.SpecialFolder.ApplicationData),
                    OperatingSystem.IsWindows() ? "Sinedo" : "sinedo");
            }
        }

        /// <summary>
        /// Path to the user profile of the current user.
        /// Here the downloads of the application are stored, the path is resolved as follows:
        /// Linux and macOS to '/home/$USER/', Windows to 'C:\Users\%USER%\'.
        /// </summary>
        /// 
        /// <exception cref="EnvironmentNotSupportedException"/>
        /// 
        /// <returns>The path to the user profile of the current user.</returns>
        public static string HomeDirectory
        {
                get => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? throw new EnvironmentNotSupportedException(Environment.SpecialFolder.UserProfile);
        }
    }
}
