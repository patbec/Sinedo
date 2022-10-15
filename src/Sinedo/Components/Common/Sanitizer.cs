using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sinedo.Components.Common
{
    public static class Sanitizer
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
        /// Entfernt illegale Zeichen aus der angegebenen Zeichenfolge.
        /// </summary>
        /// <param name="fileName">Zeichenfolge die geprüft werden soll.</param>
        /// <returns>Gibt eine sichere Zeichenfolgen zurück, die als Bezeichnung für eine Datei oder einem Ordner verwendet werden kann.</returns>
        public static string Sanitize(string fileName, char placeholder = '_')
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));


            // Illegale Zeichen für einen Ordner oder eine Datei.
            var invalidsChars = Path.GetInvalidFileNameChars();

            // Illegale Zeichen aus dem Dateinamen entfernen.
            var newName = string.Join(placeholder,
                fileName.Split(invalidsChars, StringSplitOptions.RemoveEmptyEntries));

            // Prüfen ob die Zeichenfolgen einen reservierten Namen enthält.
            if (IsReserved(newName))
                newName += placeholder + '_';

            // Neuen Name zurückgeben.
            return newName.TrimEnd('.');
        }

        /// <summary>
        /// Entfernt illegale Zeichen aus dem Pfad und gibt eine kombiniert Zeichenfolge zurück.
        /// </summary>
        /// <param name="paths">Zeichenfolgen die zu einem Pfad kombiniert werden sollen.</param>
        /// <returns>Gibt einen sicheren Pfad zurück.</returns>
        [Obsolete("Fix Bug")]
        public static string Combine(params string[] paths)
        {
            string newPath = string.Empty;

            for (int i = 0; i < paths.Length; i++)
            {
                // Illegale Zeichen aus dem Namen entfernen.
                string safeName =
                    Sanitize(paths[i]);

                // Die Zeichenfolgen erweitern.
                newPath = Path.Combine(newPath, safeName);
            }

            // Neuen Pfad zurückgeben.
            return newPath;
        }

        /// <summary>
        /// Prüft ob die angegebene Zeichenfolge einen ungültigen Namen enthält.
        /// </summary>
        /// <param name="name">Zeichenfolge die geprüft werden soll.</param>
        /// <returns>True, wenn die angegebene Zeichenfolge einen ungültigen Namen enthält.</returns>
        private static bool IsReserved(string name)
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
