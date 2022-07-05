using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace APManagerC4
{
    /// <summary>
    /// 用于对文本进行AES加密
    /// </summary>
    class AESTextEncrypter
    {
        public const int KeyLength = 256 / 8; // AES密钥长度
        public const int IVLength = 128 / 8; // IV长度

        public string Encrypt(string source)
        {
            var encryptedBytes = ProcessCore(Encoding.UTF8.GetBytes(source), _aes.CreateEncryptor());
            return Convert.ToHexString(encryptedBytes);
        }
        public string Decrypt(string encrypted)
        {
            var decryptedBytes = ProcessCore(Convert.FromHexString(encrypted), _aes.CreateDecryptor());
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        public AESTextEncrypter(byte[] key)
        {
            if (key.Length != KeyLength)
            {
                throw new ArgumentException($"Invalid key length, Only {KeyLength} allowed");
            }

            _aes.Key = key;
            _aes.IV = new byte[IVLength];
        }

        private readonly Aes _aes = Aes.Create();
        private static byte[] ProcessCore(byte[] buffer, ICryptoTransform transform)
        {
            byte[] output;
            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                {
                    using (var writer = new BufferedStream(cryptoStream))
                    {
                        writer.Write(buffer);
                    }
                    output = memoryStream.ToArray();
                }
            }
            return output;
        }
    }
}
