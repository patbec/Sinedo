using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Sinedo.Components;
using Sinedo.Flags;

namespace Sinedo.Models
{
    public record DownloadRecord
    {
        /// <summary>
        /// Anzeigename des Downloads.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Links des Downloads.
        /// </summary>
        public string[] Files { get; init; }

        /// <summary>
        /// Optinales Kennwort zum entschlüsseln des Inhaltes.
        /// </summary>
        public string Password { get; init; }

        /// <summary>
        /// Informationen über den Status.
        /// </summary>
        public DownloadState State { get; init; }

        /// <summary>
        /// Aktuelle Warteschlange oder Task.
        /// </summary>
        public string Queue { get; init; }

        /// <summary>
        /// Letzte Fehlermeldung.
        /// </summary>
        public string LastException { get; init; }

        /// <summary>
        /// Sekunden bis der Download fertigstellt wird.
        /// </summary>
        [JsonIgnore]
        public long? SecondsToComplete { get; init; }

        /// <summary>
        /// Fortschritt in Prozent.
        /// </summary>
        public int? GroupPercent { get; init; }

        /// <summary>
        /// Gelesene Bytes pro Sekunde.
        /// </summary>
        public long? BytesPerSecond { get; init; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public CancellationTokenSource Cancellation { get; init; }


        public void Save(string folderPath)
        {
            string filePath = Path.Combine(folderPath, Name + ".json");

            string fileContent = JsonSerializer.Serialize(new { Files, Password });

            File.WriteAllText(filePath, fileContent);
        }


        public static DownloadRecord Load(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            string fileContent = File.ReadAllText(filePath);

            JsonElement document = JsonSerializer.Deserialize<JsonElement>(fileContent);

            var downloadFiles = document.GetProperty(nameof(Files)).EnumerateArray().Select(item => item.GetString()).ToArray();
            var downloadPassword = document.GetProperty(nameof(Password)).GetString();

            return new DownloadRecord()
            {
                Name = fileName,
                Files = downloadFiles,
                Password = downloadPassword
            };
        }
    }
}