using System.Security.Cryptography;

namespace HospitalManage.Models
{
    public class Hybrid
    {
        private readonly Des des;
        private readonly MyRSA rsa;

        public Hybrid()
        {
            des = new Des();
            rsa = new MyRSA();
        }

        public static byte[] GetFixedDESKey()
        {
            return new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        }

        public byte[] GenerateDESKey()
        {
            byte[] key = new byte[8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        // 🔐 Hàm riêng để mã hóa khóa DES bằng RSA
        public string EncryptDESKeyWithRSA(byte[] desKey, byte[] rsaPublicKey)
        {
            string desKeyBase64 = Convert.ToBase64String(desKey);
            return rsa.Encrypt(desKeyBase64, rsaPublicKey);
        }

        // 🔐 Mã hóa lai: trả về encryptedData
        public string EncryptHybrid(string plainText, byte[] rsaPublicKey)
        {
            byte[] desKey = GetFixedDESKey(); // Dùng khóa DES cố định

            string encryptedData = des.Encrypt(plainText, desKey); // Mã hóa dữ liệu bằng DES

            return encryptedData;
        }

        // 🔓 Giải mã lai
        public string DecryptHybrid(string encryptedData, string encryptedKey, byte[] rsaPrivateKey)
        {
            try
            {
                string desKeyBase64 = rsa.Decrypt(encryptedKey, rsaPrivateKey);
                byte[] desKey = Convert.FromBase64String(desKeyBase64);

                return des.Decrypt(encryptedData, desKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi giải mã lai: " + ex.Message);
                return "[Giải mã lai thất bại]";
            }
        }
    }
}
