using System;

namespace HospitalManage.Services.Encryption
{
    public interface ISymmetricEncryptionService
    {
        // Sử dụng key/iv mặc định (ví dụ đọc từ config hoặc fallback)
        string Encrypt(string plainText);
        string Decrypt(string cipherText);

        // API cho Hybrid: mã hóa với khóa DES ngẫu nhiên, trả về cipher + key + iv
        (string CipherText, byte[] Key, byte[] IV) EncryptWithGeneratedKey(string plainText);

        // Giải mã với key/iv cụ thể
        string DecryptWithKey(string cipherText, byte[] key, byte[] iv);

        // Mã hóa với key/iv cụ thể (hữu ích nếu bạn lưu key/iv ra DB)
        string EncryptWithKey(string plainText, byte[] key, byte[] iv);
    }
}
