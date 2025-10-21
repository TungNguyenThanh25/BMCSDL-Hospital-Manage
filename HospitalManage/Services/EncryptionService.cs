using HospitalManage.Encryp;
using System.Security.Cryptography;

namespace HospitalManage.Services
{
    public class EncryptionService
    {
        private readonly RsaEncryption _rsa;
        private readonly HybridEncryption _hybrid;

        public EncryptionService()
        {
            RsaKeyStore.Initialize(); // Khởi tạo RSA key từ kho
            _rsa = new RsaEncryption(RsaKeyStore.PublicKey, RsaKeyStore.PrivateKey);
            _hybrid = new HybridEncryption();
        }

        // 🔐 Mã hóa tiền thuốc bằng AES
        public string EncryptAES(string plainText)
        {
            var aesKey = GenerateAesKey();
            var aes = new AesEncryption(aesKey);
            return aes.Encrypt(plainText);
        }

        // 🔓 Giải mã tiền thuốc bằng AES (cần truyền đúng key)
        public string DecryptAES(string encryptedText)
        {
            // Nếu bạn lưu key AES riêng, cần truyền lại key ở đây
            // Nếu không lưu key, bạn nên dùng Hybrid để giải mã tổng tiền
            throw new NotImplementedException("Giải mã AES cần key gốc hoặc lưu key.");
        }

        // 🔐 Mã hóa phí khám bằng RSA
        public string EncryptRSA(string plainText)
        {
            return _rsa.Encrypt(plainText);
        }

        // 🔓 Giải mã phí khám bằng RSA
        public string DecryptRSA(string encryptedText)
        {
            return _rsa.Decrypt(encryptedText);
        }

        // 🔐 Mã hóa tổng tiền bằng Hybrid (AES + RSA)
        public (string EncryptedData, string EncryptedKey) EncryptHybrid(string plainText)
        {
            return _hybrid.Encrypt(plainText);
        }

        // 🔓 Giải mã tổng tiền bằng Hybrid
        public string DecryptHybrid(string encryptedData, string encryptedKey)
        {
            return _hybrid.Decrypt(encryptedData, encryptedKey);
        }

        // 🔧 Tạo khóa AES ngẫu nhiên
        private string GenerateAesKey()
        {
            var key = new byte[16]; // 128-bit
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }
    }
}
