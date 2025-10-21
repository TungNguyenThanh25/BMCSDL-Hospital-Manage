using System.Text;

namespace HospitalManage.Services
{
    public static class  RSAKeyProvider
    {
        public static byte[] GetPublicKeyBytes()
        {
            string xml = File.ReadAllText("App_Data/rsa_public.xml");
            return Encoding.UTF8.GetBytes(xml);
        }

        public static byte[] GetPrivateKeyBytes()
        {
            string xml = File.ReadAllText("App_Data/rsa_private.xml");
            return Encoding.UTF8.GetBytes(xml);
        }
    }
}
