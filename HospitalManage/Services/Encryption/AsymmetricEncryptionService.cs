using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace HospitalManage.Services.Encryption
{
    public class AsymmetricEncryptionService
    {
        private readonly string _publicKeyXml;
        private readonly string _privateKeyXml;

        public AsymmetricEncryptionService(IConfiguration config)
        {
            // Đọc key từ appsettings.json
            _publicKeyXml = config["Encryption:RSA:PublicKey"]
                ?? throw new Exception("Không tìm thấy Encryption:RSA:PublicKey trong appsettings.json");
            _privateKeyXml = config["Encryption:RSA:PrivateKey"]
                ?? throw new Exception("Không tìm thấy Encryption:RSA:PrivateKey trong appsettings.json");
        }

        public string GetPublicKeyBase64() => _publicKeyXml;
        public string GetPrivateKeyBase64() => _privateKeyXml;

        public string Encrypt(string plainText, string publicKeyXml)
        {
            if (string.IsNullOrEmpty(plainText)) return null;

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml); // Import key từ XML
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string base64CipherText, string privateKeyXml)
        {
            if (string.IsNullOrEmpty(base64CipherText)) return null;

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml); // Import key từ XML
            byte[] encrypted = Convert.FromBase64String(base64CipherText);
            byte[] decrypted = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decrypted);
        }
    }

    public static class RSAExtensions
    {
        public static void FromXmlString(this RSA rsa, string xmlString)
        {
            var parameters = new RSAParameters();

            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
            xml.LoadXml(xmlString);

            if (xml.DocumentElement?.Name.Equals("RSAKeyValue") == true)
            {
                foreach (System.Xml.XmlNode node in xml.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                        case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                        case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                        case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                        case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                        case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                        case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                    }
                }
            }

            rsa.ImportParameters(parameters);
        }
    }
}
