
using System.IO;
using System.Text;
using System.Text.Json;
using Sinedo.Exceptions;

namespace Sinedo.Components
{
    public class Serializer
    {
        private readonly FileStream _fileStream;

        public Serializer(string filePath)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath));

            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public T Load<T>()
        {
            if(_fileStream.Length == 0) {
                throw new FileNotFoundException();
            }

            byte[] buffer = new byte[_fileStream.Length];
            _fileStream.Read(buffer, 0, buffer.Length);

            return JsonSerializer.Deserialize<T>(buffer);
        }

        public void Save<T>(T data)
        {
            string json = JsonSerializer.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            _fileStream.SetLength(0);
            _fileStream.Write(buffer, 0, buffer.Length);
            _fileStream.Flush();
        }
    }
}