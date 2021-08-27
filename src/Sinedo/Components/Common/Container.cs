using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components.Common
{
    /// <summary>
    /// Entschlüsselt einen Click&Load Container.
    /// 
    /// Dokumentation:
    /// https://jdownloader.org/knowledge/wiki/glossary/cnl2
    /// </summary>
    public class Container
    {
        public string Name { get; init; }
        public string Password { get; init; }
        public string Source { get; init; }
        public string[] Urls { get; init; }

        /// <summary>
        /// Entschlüsselt die angegebenen Links.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="jk">Schlüssel in Hexadecimal.</param>
        /// <param name="crypted">Verschlüsselte Links in Base64.</param>
        /// <returns>Der entschlüsselte Container.</returns>
        /// 
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="CryptographicException"></exception>
        /// 
        public static Container Decrypt(string package, string passwords, string source, string jk, string crypted)
        {
            if(string.IsNullOrWhiteSpace(package)) {
                throw new ArgumentNullException(nameof(package));
            }
            if(string.IsNullOrWhiteSpace(jk)) {
                throw new ArgumentNullException(nameof(jk));
            }
            if(string.IsNullOrWhiteSpace(crypted)) {
                throw new ArgumentNullException(nameof(crypted));
            }


            int keyStart = jk.IndexOf(Convert.ToChar(39));
            int keyEnd = jk.LastIndexOf(Convert.ToChar(39));
            if(keyStart == -1 || keyEnd == -1) {
                throw new ArgumentException("The AES key could not be read.", nameof(jk));
            }

            byte[] aesKey =  Convert.FromHexString(jk[++keyStart..keyEnd]);
            byte[] aesData = Convert.FromBase64String(crypted);

            string[] urls = DecryptStringFromBytes_Aes(aesData, aesKey).Split("\r\n");

            return new Container() {
                Name = package,
                Password = passwords,
                Source = source,
                Urls = urls
            };
        }

        private static string DecryptStringFromBytes_Aes(byte[] data, byte[] key)
        {
            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an Aes object 
            // with the specified key and IV. 
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.BlockSize = 128;
                aesAlg.Key = key;
                aesAlg.IV = key;
                aesAlg.Padding = PaddingMode.None;
                aesAlg.Mode = CipherMode.CBC;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption. 
                using var msDecrypt = new MemoryStream(data);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                // Read the decrypted bytes from the decrypting stream
                // and place them in a string.
                plaintext = srDecrypt.ReadToEnd();

            }

            return plaintext.Trim('\0');
        }
    }
}