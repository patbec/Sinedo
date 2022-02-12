using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Sinedo.Components.Common
{
    public static class ExceptionExtension
    {
        public static void Print(this Exception exception, bool stackTrace = false)
        {
            string exceptionText = GetFormattedExceptionMessage(exception);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(exceptionText);

            if (stackTrace)
            {
                Console.Write(" " + exception.StackTrace.Trim());
            }

            Console.WriteLine();
            Console.ResetColor();
        }

        public const string BOLD_TEXT = "\u001b[1m";
        public const string CONSOLE_RESET = "\u001b[0m";

        public static void Save(this Exception exception, string helpText)
        {
            try
            {
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;

                Console.WriteLine(BOLD_TEXT + "Sorry! Application crashed." + CONSOLE_RESET);
                Console.WriteLine();

                FileInfo fileInfo = GetTempFile();

                fileInfo.Directory.Create();

                File.WriteAllText(fileInfo.FullName, exception.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;

                Console.WriteLine(BOLD_TEXT + "The exception was saved to this path:" + CONSOLE_RESET);

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;

                Console.WriteLine(fileInfo.FullName + CONSOLE_RESET);

                if (helpText != null)
                {
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkRed;

                    Console.WriteLine(BOLD_TEXT + helpText + CONSOLE_RESET);
                }
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;

                Console.WriteLine(BOLD_TEXT + "The exception could not be saved in temp." + CONSOLE_RESET);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static FileInfo GetTempFile()
        {
            string folderPath = Path.GetTempPath();
            string folderName = typeof(Program).Assembly.GetName().Name;
            string fileName = string.Format("crash-{0}-{1}.txt", folderName, DateTime.UtcNow.Ticks);

            string filePath = Path.Combine(
                folderPath,
                folderName,
                fileName);

            return new FileInfo(filePath);
        }

        private static string GetFormattedExceptionMessage(Exception exception, bool fullName = false)
        {
            if (exception == null)
            {
                return string.Empty;
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
                message += Environment.NewLine + GetFormattedExceptionMessage(exception.InnerException, true);
            }

            return message;
        }
    }
}
