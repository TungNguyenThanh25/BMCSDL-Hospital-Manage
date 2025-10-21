using System.Xml.Linq;

namespace HospitalManage.Services
{
    public static class DESKeyProvider
    {
        public static byte[] GetDESKey()
        {
            string xmlPath = Path.Combine("App_Data", "des_key.xml");
            if (!File.Exists(xmlPath))
                throw new FileNotFoundException("Không tìm thấy file des_key.xml");

            XDocument doc = XDocument.Load(xmlPath);
            string hexString = doc.Root.Element("Key")?.Value;

            if (string.IsNullOrWhiteSpace(hexString))
                throw new Exception("Khóa DES trong XML bị thiếu hoặc rỗng.");

            // Chuyển từ "00-00-00-00-00-00-00-01" → byte[]
            string[] hexParts = hexString.Split('-');
            byte[] keyBytes = new byte[hexParts.Length];
            for (int i = 0; i < hexParts.Length; i++)
            {
                keyBytes[i] = Convert.ToByte(hexParts[i], 16);
            }

            return keyBytes;
        }
    }
}
