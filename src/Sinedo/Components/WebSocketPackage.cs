using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sinedo.Flags;

namespace Sinedo.Components
{
    /// <summary>
    /// Erweitertes WebSocket Paket.
    /// </summary>
    public class WebSocketPackage
    {
        /// <summary>
        /// Zusätzliche Optionen / Regeln wenn ein Objekt serialisiert wird.
        /// </summary>
        private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly byte[] buffer;

        /// <summary>
        /// Gibt die Kennung von diesem Paket zurück.
        /// </summary>
        public CommandFromClient Command
        {
            get => (CommandFromClient)buffer[0];
        }

        /// <summary>
        /// Gibt den Parameter von diesem Paket zurück.
        /// </summary>
        public byte Parameter
        {
            get => buffer[1];
        }

        /// <summary>
        /// Gibt den Inhalt von diesem Paket zurück.
        /// </summary>
        public ReadOnlySpan<byte> Content
        {
            get => buffer.AsSpan(2, buffer.Length - 2);
        }

        /// <summary>
        /// Gibt an, ob das Feld <see cref="Parameter"/> verwendet wird.
        /// </summary>
        public bool HasValidParameter
        {
            get => Parameter != PARAMETER_UNSET;
        }

        /// <summary>
        /// Erstellt ein neues erweitertes WebSocket Paket.
        /// </summary>
        /// <param name="buffer">Buffer aus dem das Paket erstellt werden soll.</param>
        /// <exception cref="ArgumentNullException">Der Buffer dark nicht Null sein.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Der Header muss 2 Bytes lang sein.</exception>
        public WebSocketPackage(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length, "No packet can be created from the buffer. (Invalid Data)");

            this.buffer = buffer;
        }

        /// <summary>
        /// Erstellt aus den angegebenen Parametern ein erweitertes WebSocket Paket.
        /// </summary>
        /// <param name="command">Kennung des Paketes.</param>
        /// <param name="parameter">Parameter des Paketes. (optional)</param>
        /// <param name="content">Inhalt des Paketes, das angegebene Objekt wird serialisiert.</param>
        public WebSocketPackage(CommandFromServer command, int parameter, object content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            MemoryStream memoryStream = new();

            memoryStream.WriteByte((byte)command);
            memoryStream.WriteByte((byte)parameter);

            var streamWriter = new Utf8JsonWriter(memoryStream);

            JsonSerializer.Serialize(streamWriter, content, content.GetType(), jsonSerializerOptions);

            // Buffer abschließen.
            streamWriter.Flush();

            // Buffer abschneiden.
            memoryStream.Capacity = (int)memoryStream.Position;

            // Keine Kopie erstellen.
            buffer = memoryStream.GetBuffer();
        }

        /// <summary>
        /// Gibt den zugrundeliegenden Buffer zurück.
        /// </summary>
        /// <returns>Byte-Array mit den Daten.</returns>
        public byte[] GetBuffer() {
            return buffer;
        }

        /// <summary>
        /// De-serialisiert den Inhalt und gibt diesen als <typeparamref name="T"/> zurück.
        /// </summary>
        /// <typeparam name="T">Objekt das aus dem Inhalt erstellt werden soll.</typeparam>
        /// <exception cref="JsonException"/>
        /// <exception cref="NotSupportedException"/>
        public T ReadContentAs<T>()
        {
            return JsonSerializer.Deserialize<T>(Content, jsonSerializerOptions);
        }

        #region Static

        /// <summary>
        /// Standardwert eines Parameters im einem erweiterten WebSocket Paket.
        /// </summary>
        public const int PARAMETER_UNSET = 0;

        #endregion
    }
}
