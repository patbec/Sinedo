using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sinedo.Components.Common
{
    public static class ExceptionExtension
    {
        //     public const string BOLD_TEXT = "\u001b[1m";
        //     public const string CONSOLE_RESET = "\u001b[0m";

        //     public const string COLOR_DARKRED = "\u001b[38;5;1m";

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

        public static void Save(this Exception exception, string helpText)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkRed;

                FileInfo fileInfo = GetTempFile();

                fileInfo.Directory.Create();

                File.WriteAllText(fileInfo.FullName, exception.ToString());

                Console.WriteLine();
                Console.WriteLine("Application crashed.");
                Console.WriteLine();
                Console.WriteLine("The exception was saved to this path:");
                Console.WriteLine(fileInfo.FullName);

                if (helpText != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(helpText);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("The exception could not be saved in temp.");
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
