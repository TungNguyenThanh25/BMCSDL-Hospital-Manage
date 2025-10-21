using HospitalManage.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Controllers
{
    [Authorize(Roles = "doctor")]
    public class MedicalRecordController : Controller
    {
        private readonly IConfiguration _configuration;

        public MedicalRecordController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /MedicalRecord/Index/1
        public IActionResult Index(int patientId)
        {
            var records = new List<MedicalRecordViewModel>();
            string patientName = "";
            string connectionString = _configuration.GetConnectionString("OracleDBConnection");

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                string patientSql = "SELECT full_name FROM patients WHERE id = :id";
                using (OracleCommand cmd = new OracleCommand(patientSql, con))
                {
                    cmd.Parameters.Add(new OracleParameter("id", patientId));
                    var result = cmd.ExecuteScalar();
                    if (result != null) patientName = result.ToString();
                }

                string recordsSql = @"
                    SELECT 
                        mr.id, p.full_name as patient_name, d.full_name as doctor_name, 
                        mr.examination_date, 
                        ENCRYPTION_PKG.decrypt_data(mr.diagnosis) AS decrypted_diagnosis,
                        mr.detailed_notes, -- Dữ liệu BLOB
                        mr.encrypted_session_key -- Khóa đã mã hóa
                    FROM medical_records mr
                    JOIN patients p ON mr.patient_id = p.id
                    JOIN doctors d ON mr.doctor_id = d.id
                    WHERE mr.patient_id = :patientId
                    ORDER BY mr.examination_date DESC";

                using (OracleCommand cmd = new OracleCommand(recordsSql, con))
                {
                    cmd.Parameters.Add(new OracleParameter("patientId", patientId));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // --- LOGIC TUẦN 7: GIẢI MÃ LAI ---
                            string decryptedDetailedNotes = "";
                            if (reader["detailed_notes"] != DBNull.Value && reader["encrypted_session_key"] != DBNull.Value)
                            {
                                byte[] encryptedData = (byte[])reader["detailed_notes"];
                                byte[] encryptedKey = (byte[])reader["encrypted_session_key"];

                                Console.WriteLine($"[DECRYPT] Kích thước khóa đọc từ DB detailed_notes: {encryptedData?.Length} bytes");
                                Console.WriteLine($"[DECRYPT] Kích thước khóa đọc từ DB encrypted_session_key: {encryptedKey?.Length} bytes");

                                decryptedDetailedNotes = DecryptHybrid(encryptedData, encryptedKey);
                            }

                            records.Add(new MedicalRecordViewModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                PatientName = reader["patient_name"].ToString(),
                                DoctorName = reader["doctor_name"].ToString(),
                                ExaminationDate = Convert.ToDateTime(reader["examination_date"]),
                                Diagnosis = reader["decrypted_diagnosis"] == DBNull.Value ? "" : reader["decrypted_diagnosis"].ToString(),
                                DetailedNotes = decryptedDetailedNotes
                            });
                        }
                    }
                }
            }
            ViewBag.PatientName = patientName;
            ViewBag.PatientId = patientId;
            return View(records);
        }

        // GET: /MedicalRecord/Create/1
        public IActionResult Create(int patientId)
        {
            var doctorId = Convert.ToInt32(User.FindFirstValue("StaffId"));
            var model = new CreateMedicalRecordViewModel { PatientId = patientId, DoctorId = doctorId };
            return View(model);
        }

        // POST: /MedicalRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateMedicalRecordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // --- LOGIC TUẦN 6: TẠO CHỮ KÝ SỐ ---
            byte[] signature = CreateDigitalSignature(model.Diagnosis, model.DoctorId);
            if (signature == null)
            {
                ModelState.AddModelError("", "Lỗi: Không thể tạo chữ ký số.");
                return View(model);
            }

            // --- LOGIC TUẦN 7: MÃ HÓA LAI ---
            (byte[] encryptedData, byte[] encryptedKey) hybridEncryptedNotes;
            if (!string.IsNullOrEmpty(model.DetailedNotes))
            {
                hybridEncryptedNotes = EncryptHybrid(model.DetailedNotes);
            }
            else
            {
                hybridEncryptedNotes = (null, null);
            }

            // --- LƯU VÀO DATABASE ---
            string connectionString = _configuration.GetConnectionString("OracleDBConnection");
            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();
                string sql = @"
                    INSERT INTO medical_records (
                        id, patient_id, doctor_id, examination_date, 
                        diagnosis, detailed_notes, encrypted_session_key, diagnosis_signature, created_at
                    ) VALUES (
                        medical_records_seq.NEXTVAL, :patient_id, :doctor_id, SYSDATE, 
                        ENCRYPTION_PKG.encrypt_data(:diagnosis), 
                        :detailed_notes, 
                        :encrypted_session_key,
                        :signature,
                        SYSTIMESTAMP
                    )";

                using (OracleCommand cmd = new OracleCommand(sql, con))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add(new OracleParameter("patient_id", model.PatientId));
                    cmd.Parameters.Add(new OracleParameter("doctor_id", model.DoctorId));
                    cmd.Parameters.Add(new OracleParameter("diagnosis", model.Diagnosis));
                    cmd.Parameters.Add(new OracleParameter("signature", OracleDbType.Raw, signature.Length) { Value = signature });

                    // Thêm tham số cho Tuần 7
                    cmd.Parameters.Add(new OracleParameter("detailed_notes", OracleDbType.Blob) { Value = hybridEncryptedNotes.encryptedData });
                    cmd.Parameters.Add(new OracleParameter("encrypted_session_key", OracleDbType.Blob) { Value = hybridEncryptedNotes.encryptedKey });

                    Console.WriteLine($"[ENCRYPT] Kích thước khóa AES đã mã hóa detailed_notes: {hybridEncryptedNotes.encryptedData?.Length} bytes");
                    Console.WriteLine($"[ENCRYPT] Kích thước khóa AES đã mã hóa encrypted_session_key: {hybridEncryptedNotes.encryptedKey?.Length} bytes");

                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index", new { patientId = model.PatientId });
        }

        #region Helper Methods for Cryptography

        // --- TUẦN 7: HÀM MÃ HÓA LAI (ĐÃ SỬA LỖI) ---
        private (byte[] encryptedData, byte[] encryptedKey) EncryptHybrid(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] encryptedData;
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
                    encryptedData = encryptor.TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length);
                }

                byte[] keyAndIv = new byte[aes.Key.Length + aes.IV.Length];
                Buffer.BlockCopy(aes.Key, 0, keyAndIv, 0, aes.Key.Length);
                Buffer.BlockCopy(aes.IV, 0, keyAndIv, aes.Key.Length, aes.IV.Length);

                byte[] encryptedKey;
                using (var rsa = RSA.Create())
                {
                    rsa.FromXmlString(_configuration["HybridEncryptionSettings:SystemPublicKey"]);
                    encryptedKey = rsa.Encrypt(keyAndIv, RSAEncryptionPadding.OaepSHA256);
                }

                return (encryptedData, encryptedKey);
            }
        }

        // --- TUẦN 7: HÀM GIẢI MÃ LAI (ĐÃ SỬA LỖI) ---
        private string DecryptHybrid(byte[] encryptedData, byte[] encryptedKey)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;

                byte[] keyAndIv;
                // SỬA LỖI: Sử dụng RSA.Create() thay vì new RSACryptoServiceProvider()
                using (var rsa = RSA.Create())
                {
                    rsa.FromXmlString(_configuration["HybridEncryptionSettings:SystemPrivateKey"]);
                    keyAndIv = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
                }

                byte[] key = new byte[32];
                byte[] iv = new byte[16];
                Buffer.BlockCopy(keyAndIv, 0, key, 0, 32);
                Buffer.BlockCopy(keyAndIv, 32, iv, 0, 16);

                using (var decryptor = aes.CreateDecryptor(key, iv))
                {
                    byte[] decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
        }

        // --- TUẦN 6: HÀM TẠO CHỮ KÝ SỐ (CẬP NHẬT CHO NHẤT QUÁN) ---
        private byte[] CreateDigitalSignature(string diagnosis, int doctorId)
        {
            string privateKeyXml = GetDoctorPrivateKey(doctorId);
            if (string.IsNullOrEmpty(privateKeyXml)) return null;

            // SỬA LỖI: Sử dụng RSA.Create() cho nhất quán và tương thích đa nền tảng
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privateKeyXml);
                var dataToSign = Encoding.UTF8.GetBytes(diagnosis);
                return rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        private string GetDoctorPrivateKey(int doctorId)
        {
            if (doctorId == 1) return "<RSAKeyValue><Modulus>zTym3H4hRVp81vgjkDfmvF4Ekf7htrgExUnsGBPWAYNduGfuucYWho09mwihhNvzz9xAyqLQC8djIm8RY0KRZQcRvwSqgSWagqU9YPjBkFoJ8ltq/KsOo/GUHt+d8XVC/h6msjnakqr2zGIExhZV136JmkC9wD7TsHmXkCzvwVE=</Modulus><Exponent>AQAB</Exponent><P>5196SwMP3DC/H+x31p5mL7PADuHyIB7G2zQ87mTDgarlg7y1hQJaDH1CsWi8fRmRmcgOCADPAu4wsZY1qlGQ5w==</P><Q>4xUC3YJt4gNMxAQaP3JtzLhXyEMrUKSU0/+VU8p25Ta3PwTEmA//590qA7Ku3M+ik/eGFtZm8B4+jWsKhOx9Bw==</Q><DP>fXBzMfXwBFXdWOZwNkhcaGJQrwDqr2VgNHnGywyQPl2z309RLlKPFZRXsy1we3aATNp8WPRvR0xx0+X3JGbiYw==</DP><DQ>qXv7IXzBqpiv6PTu6j/rt4o26l9Hqu7Lrdbqixln1/gYmM5kNOJsK5AkVZI9dMz8GNf7mnv3ZGwOX9puhXtEbQ==</DQ><InverseQ>KiQznga252bDbmTjCTD8hMhhzEtpN9U+KA1hD3BE9bbUCVTPTrqV9dNu1rbsdUsD4iCcj1SGB2z4jXmNFZuGqw==</InverseQ><D>JrwovfqsKtu+LhBdHe3/BVQ1Rpy1Wvf2JooiHhU4UcbKXHB5NOS3AaQMmYMSgPHGXVezfssluuNhyXDu9i3tJK6e+3wiHDdatlUbOvy74IFGtqQ6T7w/0x578FnSOIhlRqvCYknEXDfS4ZfP3wkq89PenTOuwDE3JQ96qRQ3gqU=</D></RSAKeyValue>";
            return null;
        }

        #endregion
    }
}