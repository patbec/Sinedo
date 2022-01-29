using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sinedo.Components;
using Sinedo.Flags;
using Sinedo.Models;

namespace Sinedo.Singleton
{
    public class HyperlinkManager
    {
        private readonly WebSocketBroadcaster broadcaster;
        private readonly ILogger<HyperlinkManager> logger;
        private readonly Serializer linksFile;

        private List<HyperlinkRecord> links = new();

        public HyperlinkManager(WebSocketBroadcaster broadcaster, ILogger<HyperlinkManager> logger)
        {
            this.broadcaster = broadcaster;
            this.logger = logger;

            string linksPath = Path.Combine(AppDirectories.ConfigDirectory, "links.json");

            linksFile = new Serializer(linksPath);

            try
            {
                HyperlinkRecord[] linksData = linksFile.Load<HyperlinkRecord[]>();
                links = linksData.ToList();

                logger.LogInformation("Links file loaded successfully.");
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException)
                {
                    logger.LogInformation("A new links file will be created.");

                    // Standard-Link bei einer neuen Datei.
                    links.Add(new()
                    {
                        DisplayName = "GitHub",
                        Url = "https://github.com/patbec/Sinedo" // Move to config
                    });
                    linksFile.Save(links);
                }
                else
                {
                    logger.LogCritical(ex, "The saved links could not be loaded.");
                }
            }
        }

        /// <summary>
        /// Speichert eine neue Auflistung von Links und benachrichtigt alle verbundenen Clients.
        /// </summary>
        /// <param name="data">Auflistung mit neuen Links.</param>
        public void SetLinks(HyperlinkRecord[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            // ToDo: Use WriteLock
            lock (this)
            {
                // Ungültige Links herausfiltern.
                data = data.Where(d => !string.IsNullOrWhiteSpace(d.Url)).ToArray();

                // Ungültige Anzeigenamen mit dem Link überschreiben.
                foreach (var item in data)
                {
                    if (string.IsNullOrWhiteSpace(item.DisplayName))
                    {
                        item.DisplayName = item.Url;
                    }
                }

                // Speichern und übernehmen.
                linksFile.Save(data);
                links = data.ToList();

                logger.LogDebug("New links have been saved.");

                // Verbundene Clients über die neuen Links benachrichtigen.
                broadcaster.Add(CommandFromServer.Links, data);
            }
        }

        /// <summary>
        /// Ruft die aktuell gespeicherten Links ab.
        /// </summary>
        /// <returns>Auflistung von gespeicherten Links.</returns>
        public HyperlinkRecord[] GetData()
        {
            lock (this) // ToDo: Fix Thread Issues
            {
                return links.ToArray();
            }
        }
    }
}