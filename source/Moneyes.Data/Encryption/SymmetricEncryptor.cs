using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Moneyes.Data
{
    public static class SymmetricEncryptor
    {
        private const int AesBlockByteSize = 128 / 8;
        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

        public static string EncryptString(string toEncrypt, string password)
        {
            if (toEncrypt is null)
            {
                return null;
            }

            try
            {
                var key = GetKey(password);

                using (var aes = Aes.Create())
                {
                    var iv = GenerateRandomBytes(AesBlockByteSize);

                    using (var encryptor = aes.CreateEncryptor(key, iv))
                    {
                        var plainText = Encoding.UTF8.GetBytes(toEncrypt);
                        var cipherText = encryptor
                            .TransformFinalBlock(plainText, 0, plainText.Length);

                        var result = new byte[iv.Length + cipherText.Length];
                        iv.CopyTo(result, 0);
                        cipherText.CopyTo(result, iv.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static string DecryptToString(string encryptedString, string password)
        {
            if (encryptedString is null)
            {
                return null;
            }

            try
            {
                byte[] encryptedData = Convert.FromBase64String(encryptedString);
                var key = GetKey(password);

                using (var aes = Aes.Create())
                {
                    var iv = encryptedData.Take(AesBlockByteSize).ToArray();
                    var cipherText = encryptedData.Skip(AesBlockByteSize).ToArray();

                    using (var encryptor = aes.CreateDecryptor(key, iv))
                    {
                        var decryptedBytes = encryptor
                            .TransformFinalBlock(cipherText, 0, cipherText.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static byte[] GetKey(string password)
        {
            var keyBytes = Encoding.UTF8.GetBytes(password);
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(keyBytes);
            }
        }

        private static byte[] GenerateRandomBytes(int numberOfBytes)
        {
            var randomBytes = new byte[numberOfBytes];
            Random.GetBytes(randomBytes);
            return randomBytes;
        }
    }
}