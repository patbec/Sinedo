using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace Tests
{
    [Collection("Sequential")]
    public class UnitTest1
    {
        private readonly int BYTES_20MB = 20971520;

        [Theory(DisplayName = "ReadWrite Buffer Test")]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        public void BufferTest(int bufferSize)
        {
            byte[] sourceBuffer = new byte[BYTES_20MB];

            // Fill buffer with random bytes:
            new Random(DateTime.Now.Millisecond).NextBytes(sourceBuffer);

            Stream sourceStream = new MemoryStream(sourceBuffer);
            Stream targetStream = new MemoryStream(BYTES_20MB);


            long byteReadTotal = 0;

            // Jumbo-Buffer erstellen.
            byte[] buffer = new byte[bufferSize];

            // Anzahl der gelesenen Bytes in einer Sequenz.
            int bytesRead;

            var timeStart = DateTime.Now.Ticks;

            for (int i = 0; i < 5; i++)
            {
                // Kopieren bis keine Bytes mehr gelesen wurden.
                while ((bytesRead = sourceStream.Read(buffer,0, buffer.Length)) > 0)
                {
                    // Buffer in die Datei schreiben.
                    targetStream.Write(buffer, 0, bytesRead);

                    Interlocked.Add(ref byteReadTotal, bytesRead);
                }
            }

            var timeEnd = DateTime.Now.Ticks;
            var timeDiff = timeEnd - timeStart;

            Console.WriteLine($"BufferTest[{bufferSize}]: " + FormatTime(timeDiff));
        }

        private string FormatTime(long timeDiff) {
            return timeDiff * 100 + " ns";
        }
    }
}
