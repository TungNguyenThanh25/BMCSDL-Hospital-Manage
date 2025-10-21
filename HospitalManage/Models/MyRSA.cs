using System;
using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Models
{
    public class MyRSA
    {
        public MyRSA() { }
        // Mã hóa RSA - dùng public key
        public string Encrypt(string plainText, byte[] publicKeyXmlBytes)
        {
            try
            {
                string publicKeyXml = Encoding.UTF8.GetString(publicKeyXmlBytes);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(publicKeyXml);

                    byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedData = rsa.Encrypt(dataToEncrypt, false); // false = PKCS#1 v1.5 padding

                    return Convert.ToBase64String(encryptedData);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi mã hóa RSA: " + ex.Message);
            }
        }
        public bool IsBase64String(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            Span<byte> buffer = new Span<byte>(new byte[input.Length]);
            return Convert.TryFromBase64String(input, buffer, out _);
        }
        // Giải mã RSA - dùng private key
        public string Decrypt(string base64Encrypted, byte[] privateKeyXmlBytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64Encrypted))
                    throw new Exception("Chuỗi đầu vào trống.");

                if (!IsBase64String(base64Encrypted))
                    throw new Exception("Chuỗi đầu vào không phải là Base64 hợp lệ.");

                string privateKeyXml = Encoding.UTF8.GetString(privateKeyXmlBytes);
                if (string.IsNullOrWhiteSpace(privateKeyXml) || !privateKeyXml.Contains("<RSAKeyValue>"))
                    throw new Exception("Khóa riêng không hợp lệ hoặc bị thiếu.");

                byte[] encryptedBytes = Convert.FromBase64String(base64Encrypted);

                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(privateKeyXml);

                    try
                    {
                        byte[] decryptedData = rsa.Decrypt(encryptedBytes, false);
                        return Encoding.UTF8.GetString(decryptedData);
                    }
                    catch (CryptographicException)
                    {
                        throw new Exception("Giải mã thất bại: dữ liệu không khớp với khóa RSA.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi giải mã RSA: " + ex.Message);
            }
        }



    }
}
