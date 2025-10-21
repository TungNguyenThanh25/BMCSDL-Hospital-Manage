using System.Security.Cryptography;

namespace HospitalManage.Encryp
{
    public static class RsaKeyStore
    {
        public static RSAParameters PublicKey { get; private set; }
        public static RSAParameters PrivateKey { get; private set; }

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            using var rsa = RSA.Create(2048); // ✅ Khóa 2048-bit an toàn
            PublicKey = rsa.ExportParameters(false); // Chỉ public
            PrivateKey = rsa.ExportParameters(true); // Cả public + private

            _initialized = true;
        }
    }
}
