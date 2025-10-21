using System;
using System.Text;

namespace HospitalManage.Services.Encryption
{
    public class HybridEncryptionService
    {
        private readonly AsymmetricEncryptionService _rsa;
        private readonly ISymmetricEncryptionService _des;

        public HybridEncryptionService(AsymmetricEncryptionService rsa, ISymmetricEncryptionService des)
        {
            _rsa = rsa;
            _des = des;
        }

        // Mã hóa hybrid: DES mã hóa data, RSA mã hóa (base64) của (key + iv)
        // Trả về cipherText (DES base64) và encryptedKey (RSA base64)
        public (string CipherText, string EncryptedKey) EncryptHybrid(string plainText, string base64PublicKey)
        {
            // 1) Tạo DES key/iv ngẫu nhiên và mã hóa dữ liệu
            var (cipherText, key, iv) = _des.EncryptWithGeneratedKey(plainText);

            // 2) Kết hợp key + iv -> base64 string để RSA mã hóa
            byte[] combined = new byte[key.Length + iv.Length];
            Buffer.BlockCopy(key, 0, combined, 0, key.Length);
            Buffer.BlockCopy(iv, 0, combined, key.Length, iv.Length);
            string combinedBase64 = Convert.ToBase64String(combined);

            // 3) RSA mã hóa combinedBase64 bằng public key
            string encryptedKey = _rsa.Encrypt(combinedBase64, base64PublicKey);

            return (cipherText, encryptedKey);
        }

        // Giải mã hybrid: RSA giải encryptedKey -> base64(key+iv) -> dùng DES giải cipherText
        public string DecryptHybrid(string cipherText, string encryptedKey, string base64PrivateKey)
        {
            // 1) RSA giải encryptedKey -> nhận lại combinedBase64
            string combinedBase64 = _rsa.Decrypt(encryptedKey, base64PrivateKey);
            byte[] combined = Convert.FromBase64String(combinedBase64);

            // 2) tách key và iv (DES dùng 8 bytes cho key và iv)
            int half = 8; // DES requires 8 bytes key and 8 bytes iv
            byte[] key = new byte[half];
            byte[] iv = new byte[half];
            Buffer.BlockCopy(combined, 0, key, 0, half);
            Buffer.BlockCopy(combined, half, iv, 0, half);

            // 3) Dùng DES giải cipherText
            return _des.DecryptWithKey(cipherText, key, iv);
        }

        // Lấy public/private key từ RSA service (dùng cho client/server)
        public string GetPublicKeyBase64() => _rsa.GetPublicKeyBase64();
        public string GetPrivateKeyBase64() => _rsa.GetPrivateKeyBase64();
    }
}
