using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Sinedo.Models
{
    public record DiscoveryRecord
    {
        public string DisplayName { get; init; }
        public string[] Urls { get; init; }

        public static DiscoveryRecord Parse(byte[] data)
        {
            try
            {
                return JsonSerializer.Deserialize<DiscoveryRecord>(data);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gibt das Objekt als Zeichenfolge in lesbarer Form zurück.
        /// </summary>
        /// <remarks>
        /// Beispiel Ausgabe:
        /// Name:
        ///   Sinedo Test Server
        /// Urls:
        ///   https://my-sinedo-server.com (Recommended)
        ///   
        /// Name:
        ///   Sinedo Test Server
        /// Urls:
        ///   http://sinedo.local (Recommended)
        ///   http://192.168.178.5
        ///   http://[1080:0:0:0:8:800:200C:417A]
        ///   
        /// Name:
        ///   Sinedo Test Server
        /// Urls:
        ///   <empty>
        /// </remarks>
        /// <returns></returns>
        public override string ToString()
        {
            using StringWriter stringWriter = new();

            string displayName;

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                displayName = "<empty>";
            }
            else
            {
                displayName = DisplayName;
            }

            stringWriter.WriteLine("Name:");
            stringWriter.WriteLine(CommandLine.TAB + displayName);

            if (Urls == null || Urls.Length == 0)
            {
                stringWriter.WriteLine("Urls:");
                stringWriter.WriteLine(CommandLine.TAB + "<empty>");
            }
            else
            {
                stringWriter.WriteLine("Urls:");

                for (int i = 0; i < Urls.Length; i++)
                {
                    stringWriter.Write(CommandLine.TAB + Urls[i]);

                    if (i == 0)
                    {
                        stringWriter.Write(" (Recommended)");
                    }
                    stringWriter.WriteLine();
                }
            }

            return stringWriter.ToString();
        }


        // private string GetBestUrl() {
        //     string bestUrl = "";
        //     double bestScore = 0;

        //     foreach (var url in Urls)
        //     {
        //         double score = 0;
        //         switch (Uri.CheckHostName(url))
        //         {
        //             case UriHostNameType.Dns:
        //                 {
        //                     score = 10000; break;
        //                 }
        //             case UriHostNameType.IPv6:
        //                 {
        //                     score = 1000; break;
        //                 }
        //             case UriHostNameType.IPv4:
        //                 {
        //                     score = 100; break;
        //                 }
        //             case UriHostNameType.Basic:
        //                 {
        //                     score = 10; break;
        //                 }
        //             case UriHostNameType.Unknown:
        //                 {
        //                     score = 1; break;
        //                 }
        //         }

        //         if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        //         {
        //             if (uri.IsFile)
        //             {
        //                 score /= 0.5;
        //             }
        //             else if (uri.Scheme == Uri.UriSchemeHttps)
        //             {
        //                 score *= 0.5;
        //             }
        //         }

        //         if (score <= bestScore)
        //         {
        //             bestUrl = url;
        //             bestScore = score;
        //         }
        //     }

        //     stringWriter.WriteLine("Recommended Url:");
        //     stringWriter.WriteLine(TAB + bestUrl);
        //     stringWriter.WriteLine();

        //     stringWriter.WriteLine("Urls:");

        //     // Urls nach Typ gruppieren und ausgeben.
        //     foreach (var group in Urls.GroupBy(n => Uri.CheckHostName(n)))
        //     {
        //         // Gruppennamen: Dns, IPv4, IPv6, Basic, Unknown
        //         foreach (string url in group)
        //         {
        //             if (url != bestUrl)
        //             {
        //                 stringWriter.WriteLine(TAB + url);
        //             }
        //         }
        //     }
        //     stringWriter.WriteLine();
        // }
    }
}