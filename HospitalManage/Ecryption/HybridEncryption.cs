using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Encryp
{
    public class HybridEncryption
    {
        private readonly RsaEncryption _rsa;

        public HybridEncryption()
        {
            _rsa = new RsaEncryption(RsaKeyStore.PublicKey, RsaKeyStore.PrivateKey);
        }

        public (string EncryptedData, string EncryptedKey) Encrypt(string plainText)
        {
            var aesKeyBytes = GenerateRandomBytes(16);
            var aesKeyBase64 = Convert.ToBase64String(aesKeyBytes);

            var aes = new AesEncryption(aesKeyBase64);
            var encryptedData = aes.Encrypt(plainText);
            var encryptedKey = _rsa.Encrypt(aesKeyBase64);

            return (encryptedData, encryptedKey);
        }

        public string Decrypt(string encryptedData, string encryptedKey)
        {
            var aesKeyBase64 = _rsa.Decrypt(encryptedKey);
            var aes = new AesEncryption(aesKeyBase64);
            return aes.Decrypt(encryptedData);
        }

        private byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }
    }
}
