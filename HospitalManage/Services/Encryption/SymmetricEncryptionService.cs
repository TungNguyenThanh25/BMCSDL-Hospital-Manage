using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Services.Encryption
{
    public class SymmetricEncryptionService : ISymmetricEncryptionService
    {
        private readonly byte[] _defaultKey; // 8 bytes
        private readonly byte[] _defaultIv;  // 8 bytes

        public SymmetricEncryptionService(IConfiguration configuration)
        {
            // Đọc key/iv từ cấu hình nếu có, ngược lại dùng fallback (8 ký tự)
            string keyStr = configuration["Encryption:DES:Key"] ?? "12345678";
            string ivStr = configuration["Encryption:DES:IV"] ?? "87654321";

            _defaultKey = EnsureLength(Encoding.UTF8.GetBytes(keyStr), 8);
            _defaultIv = EnsureLength(Encoding.UTF8.GetBytes(ivStr), 8);
        }

        // Đảm bảo mảng byte đúng độ dài (pad hoặc trim)
        private static byte[] EnsureLength(byte[] input, int length)
        {
            if (input.Length == length) return input;
            var outBytes = new byte[length];
            if (input.Length > length)
            {
                Array.Copy(input, outBytes, length);
            }
            else
            {
                Array.Copy(input, outBytes, input.Length);
                // pad bằng 0 nếu cần (an toàn cho ví dụ học tập)
                for (int i = input.Length; i < length; i++) outBytes[i] = 0x0;
            }
            return outBytes;
        }

        public string Encrypt(string plainText)
        {
            return EncryptWithKey(plainText, _defaultKey, _defaultIv);
        }

        public string Decrypt(string cipherText)
        {
            return DecryptWithKey(cipherText, _defaultKey, _defaultIv);
        }

        public (string CipherText, byte[] Key, byte[] IV) EncryptWithGeneratedKey(string plainText)
        {
            using var des = new DESCryptoServiceProvider();
            des.GenerateKey();  
            des.GenerateIV();

            var cipher = EncryptWithKey(plainText, des.Key, des.IV);
            return (cipher, des.Key, des.IV);
        }

        public string EncryptWithKey(string plainText, byte[] key, byte[] iv)
        {
            if (plainText == null) return null;
            using var des = new DESCryptoServiceProvider();
            des.Key = EnsureLength(key, 8);
            des.IV = EnsureLength(iv, 8);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(plainBytes, 0, plainBytes.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }

        public string DecryptWithKey(string cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null) return null;
            using var des = new DESCryptoServiceProvider();
            des.Key = EnsureLength(key, 8);
            des.IV = EnsureLength(iv, 8);

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
