using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components.Common
{
    public static class PathHelper
    {
        /// <summary>
        /// Gibt ein Array von Zeichenfolgen zurück, die nicht als Bezeichnung für eine Datei oder einem Ordner angegeben werden können.
        /// </summary>
        public static readonly string[] InvalidNames = new[]
        {
            "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        /// <summary>
        /// Entfernt illegale Zeichen aus dem angegebenen Dateinamen.
        /// </summary>
        /// <param name="unsafeName">Zeichenfolge die geprüft werden soll.</param>
        /// <returns>Gibt eine sichere Zeichenfolgen zurück, die als Bezeichnung für eine Datei oder einem Ordner verwendet werden kann.</returns>
        public static string Sanitize(string fileName, string placeholder = " ")
        {
            if(string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));


            // Illegale Zeichen für einen Dateinamen.
            var invalidsChars = Path.GetInvalidFileNameChars();

            // Illegale Zeichen aus dem Dateinamen entfernen.
            var newName = string.Join(placeholder, fileName.Split(invalidsChars, StringSplitOptions.RemoveEmptyEntries));

            // Prüfen ob die Zeichenfolgen einen reservierten Namen enthält.
            if (HasInvalidName(newName))
                newName += placeholder + "0";

            // Neuen Name zurückgeben.
            return newName.TrimEnd('.', ' ');
        }

        /// <summary>
        /// Prüft ob die angegebene Zeichenfolge einen ungültigen Namen enthält.
        /// </summary>
        /// <param name="name">Zeichenfolge die geprüft werden soll.</param>
        /// <returns>True, wenn die angegebene Zeichenfolge einen ungültigen Namen enthält.</returns>
        private static bool HasInvalidName(string name)
        {
            foreach (string reservedWord in InvalidNames)
            {
                if (name == reservedWord)
                    return true;
            }

            return false;
        }
    }
}
