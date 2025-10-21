using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Models
{
    public class Des
    {
        byte[] IV = { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
        //0x0 là 0 trong hệ thập lục phân
        public string Encrypt(string plainText, byte[] key)
        {
            try
            {
                using (MemoryStream mStream = new MemoryStream())
                using (DES des = DES.Create())
                using (ICryptoTransform encryptor = des.CreateEncryptor(key, IV))
                using (var cStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                {
                    byte[] toEncrypt = Encoding.UTF8.GetBytes(plainText);
                    cStream.Write(toEncrypt, 0, toEncrypt.Length);
                    cStream.FlushFinalBlock();

                    byte[] encryptedBytes = mStream.ToArray();
                    return Convert.ToBase64String(encryptedBytes); // ✅ Trả về Base64
                }
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }


        public string Decrypt(string base64Encrypted, byte[] key)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(base64Encrypted); // ✅ Giải mã base64

                using (MemoryStream mStream = new MemoryStream(encryptedBytes))
                using (DES des = DES.Create())
                using (ICryptoTransform decryptor = des.CreateDecryptor(key, IV))
                using (var cStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                using (MemoryStream resultStream = new MemoryStream())
                {
                    cStream.CopyTo(resultStream);
                    byte[] decryptedBytes = resultStream.ToArray();
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine("Lỗi giải mã: " + ex.Message);
                return "[Giải mã thất bại]";
            }
            catch (FormatException ex)
            {
                Console.WriteLine("Chuỗi Base64 không hợp lệ: " + ex.Message);
                return "[Chuỗi mã hóa không hợp lệ]";
            }
        }
    }
}
