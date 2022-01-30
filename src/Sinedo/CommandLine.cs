using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sinedo.Background;
using Sinedo.Components;
using Sinedo.Components.Logging;
using Sinedo.Exceptions;
using Sinedo.Models;
using Sinedo.Singleton;

namespace Sinedo
{
    public class CommandLine
    {
        public const string TAB = "  ";
        public const string BOLD_TEXT = "\u001b[38;1m";
        public const string COLOR_ORANGE = "\u001b[38;5;208m";
        public const string CONSOLE_RESET = "\u001b[0m";

        private readonly static string[] HELP_TEXT = {
            COLOR_ORANGE +
            BOLD_TEXT +
            @"   _____ _                __      ",
            @"  / ___/(_)___  ___  ____/ /___   ",
            @"  \__ \/ / __ \/ _ \/ __  / __ \  ",
            @" ___/ / / / / /  __/ /_/ / /_/ /  ",
            @"/____/_/_/ /_/\___/\__,_/\____/   ",
            CONSOLE_RESET +
            @"",
            BOLD_TEXT + "Support:" + CONSOLE_RESET,
            @"  https://github.com/patbec/Sinedo",
            @"",
            BOLD_TEXT + "Description:" + CONSOLE_RESET,
            @"  Your Simple Network Downloader!",
            @"",
            BOLD_TEXT + "Usage:" + CONSOLE_RESET,
            @"  sinedo [option]",
            @"",
            BOLD_TEXT + "Options:" + CONSOLE_RESET,
            @"  -c, --check        Checks the settings file for errors.",
            @"  -s, --search       Searches the network for other servers for 5 seconds.",
            @"  -w, --worker       Starts the server as a service.",
            @"  -v, --version      Show version information",
            @"  -?, -h, --help     Show help and usage information"
        };

        private readonly string[] args;

        public CommandLine(string[] args)
        {
            this.args = args;
        }

        public async Task<bool> ExecuteAsync()
        {
            // Workround for https://github.com/dotnet/aspnetcore/issues/31365
#if DEBUG
            if (args.Length == 0)
            {
                return false;
            }
#else
#endif
            string argument = args.FirstOrDefault();

            if (argument != null)
            {
                argument = argument.Trim();
            }

            try
            {
                switch (argument)
                {
                    case "--check":
                    case "-c":
                        {
                            Configuration.LoadFile();
                            break;
                        }
                    case "--search":
                    case "-s":
                        {
                            await SearchForServerAsync();
                            break;
                        }
                    case "--worker":
                    case "-w":
                        {
                            return false;
                        }
                    case "--version":
                    case "-v":
                        {
                            Console.WriteLine("sinedo " + SystemRecord.GetSystemInfo());
                            break;
                        }
                    default:
                        {
                            foreach (string line in HELP_TEXT)
                            {
                                Console.WriteLine(line);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(GetErrorMessage(ex));
                Console.ResetColor();

                Environment.ExitCode = 10;
            }

            return true;
        }

        private static string GetErrorMessage(Exception exception, bool fullName = false)
        {
            if (exception == null)
            {
                return "";
            }

            string typeName;
            string errorMessage = exception.Message;

            if (fullName)
            {
                typeName = exception.GetType().FullName;
            }
            else
            {
                typeName = exception.GetType().Name;
            }

            string message = $"{typeName}: ❌ {errorMessage}";

            if (exception.InnerException != null)
            {
                message += Environment.NewLine + GetErrorMessage(exception.InnerException, true);
            }

            return message;
        }

        private static async Task SearchForServerAsync()
        {
            var count = 0;
            try
            {
                Console.WriteLine("Search 5 seconds for servers...");
                Console.WriteLine();

                var cancellationTokenSource = new CancellationTokenSource(5000);

                await foreach (DiscoveryRecord discoveryInfo in AutoDiscovery.FindServerAsync(cancellationTokenSource.Token))
                {
                    if (discoveryInfo != null)
                    {
                        Console.WriteLine(discoveryInfo);
                        count++;
                    }
                    else
                    {
                        Console.WriteLine("<Invalid Response>");
                    }
                }
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                Console.WriteLine(count + " server(s) were found.");
            }
        }
    }
}
