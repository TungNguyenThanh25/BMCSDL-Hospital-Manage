using HospitalManage.Models;
using HospitalManage.Services.Encryption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text;

namespace HospitalManage.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly string _connectionString;
        private readonly HybridEncryptionService _hybridService;

        public PatientController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDBConnection");

            var rsa = new AsymmetricEncryptionService(configuration);
            var des = new SymmetricEncryptionService(configuration); 
            _hybridService = new HybridEncryptionService(rsa, des);
        }

        // ------------------ DANH SÁCH BỆNH NHÂN ------------------
        public IActionResult Index()
        {
            var patients = new List<Patient>();

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                string query = @"
            SELECT id, full_name, birth_date, gender,
                   ENCRYPTION_PKG.decrypt_data(address) AS address,
                   created_at,
                   id_number, phone_number, id_number_key, phone_number_key
            FROM patients
            ORDER BY id DESC";

                using var cmd = new OracleCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    // decrypt id_number & phone_number bằng hybrid service như trước
                    string? idCipher = reader["id_number"]?.ToString();
                    string? idKey = reader["id_number_key"]?.ToString();
                    string? phoneCipher = reader["phone_number"]?.ToString();
                    string? phoneKey = reader["phone_number_key"]?.ToString();

                    string? decryptedId = null;
                    string? decryptedPhone = null;

                    try
                    {
                        if (!string.IsNullOrEmpty(idCipher) && !string.IsNullOrEmpty(idKey))
                            decryptedId = _hybridService.DecryptHybrid(idCipher, idKey, _hybridService.GetPrivateKeyBase64());

                        if (!string.IsNullOrEmpty(phoneCipher) && !string.IsNullOrEmpty(phoneKey))
                            decryptedPhone = _hybridService.DecryptHybrid(phoneCipher, phoneKey, _hybridService.GetPrivateKeyBase64());
                    }
                    catch
                    {
                        decryptedId = "[Giải mã lỗi]";
                        decryptedPhone = "[Giải mã lỗi]";
                    }

                    patients.Add(new Patient
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        FullName = reader["full_name"]?.ToString(),
                        BirthDate = reader["birth_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["birth_date"]),
                        Gender = reader["gender"]?.ToString(),
                        Address = reader["address"] == DBNull.Value ? null : reader["address"].ToString(),
                        IdNumber = decryptedId,
                        PhoneNumber = decryptedPhone,
                        CreatedAt = reader["created_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["created_at"])
                    });
                }
            }
            catch (OracleException ex)
            {
                TempData["Error"] = $"Lỗi truy xuất dữ liệu: {ex.Message}";
            }

            return View(patients);
        }

        // ------------------ THÊM BỆNH NHÂN ------------------
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Patient model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                // Mã hóa lai cho id & phone như trước
                string publicKey = _hybridService.GetPublicKeyBase64();
                var idEncrypted = _hybridService.EncryptHybrid(model.IdNumber ?? "", publicKey);
                var phoneEncrypted = _hybridService.EncryptHybrid(model.PhoneNumber ?? "", publicKey);

                // PL/SQL block gọi function ENCRYPTION_PKG.encrypt_data(:Address)
                string plsql = @"
BEGIN
    INSERT INTO patients (
        id, full_name, birth_date, gender, address, created_at,
        id_number, phone_number, id_number_key, phone_number_key
    )
    VALUES (
        patients_seq.NEXTVAL,
        :FullName,
        :BirthDate,
        :Gender,
        ENCRYPTION_PKG.encrypt_data(:Address),
        :CreatedAt,
        :IdCipher,
        :PhoneCipher,
        :IdKey,
        :PhoneKey
    );
END;";

                using var tx = conn.BeginTransaction();
                using var cmd = new OracleCommand(plsql, conn)
                {
                    BindByName = true,
                    Transaction = tx
                };

                // Thêm tham số với OracleDbType và kích thước hợp lý
                cmd.Parameters.Add(new OracleParameter("FullName", OracleDbType.Varchar2, model.FullName ?? (object)DBNull.Value, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("BirthDate", OracleDbType.Date, (object?)model.BirthDate ?? DBNull.Value, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("Gender", OracleDbType.Varchar2, (object?)model.Gender ?? DBNull.Value, ParameterDirection.Input));
                // :Address được truyền là VARCHAR2; function ENCRYPTION_PKG sẽ convert và trả RAW để lưu vào cột RAW
                cmd.Parameters.Add(new OracleParameter("Address", OracleDbType.Varchar2, model.Address ?? (object)DBNull.Value, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("CreatedAt", OracleDbType.TimeStamp, DateTime.Now, ParameterDirection.Input));

                // Các tham số ciphertext / keys (các cột của bạn là VARCHAR2)
                var pIdCipher = new OracleParameter("IdCipher", OracleDbType.Varchar2, idEncrypted.CipherText ?? (object)DBNull.Value, ParameterDirection.Input);
                pIdCipher.Size = 2000; // điều chỉnh theo chiều dài tối đa dữ liệu bạn lưu
                cmd.Parameters.Add(pIdCipher);

                var pPhoneCipher = new OracleParameter("PhoneCipher", OracleDbType.Varchar2, phoneEncrypted.CipherText ?? (object)DBNull.Value, ParameterDirection.Input);
                pPhoneCipher.Size = 2000;
                cmd.Parameters.Add(pPhoneCipher);

                var pIdKey = new OracleParameter("IdKey", OracleDbType.Varchar2, idEncrypted.EncryptedKey ?? (object)DBNull.Value, ParameterDirection.Input);
                pIdKey.Size = 2000;
                cmd.Parameters.Add(pIdKey);

                var pPhoneKey = new OracleParameter("PhoneKey", OracleDbType.Varchar2, phoneEncrypted.EncryptedKey ?? (object)DBNull.Value, ParameterDirection.Input);
                pPhoneKey.Size = 2000;
                cmd.Parameters.Add(pPhoneKey);

                cmd.ExecuteNonQuery();
                tx.Commit();

                TempData["Success"] = "Thêm bệnh nhân thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (OracleException ex)
            {
                // Nên log ex chi tiết ở nơi phù hợp
                ModelState.AddModelError("", $"Lỗi khi thêm bệnh nhân: {ex.Message}");
                return View(model);
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(model);
            }
        }


        // ------------------ CHỈNH SỬA BỆNH NHÂN ------------------
        public IActionResult Edit(int id)
        {
            Patient? model = null;

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                string query = @"
            SELECT id, full_name, birth_date, gender,
                   ENCRYPTION_PKG.decrypt_data(address) AS address,
                   created_at,
                   id_number, phone_number, id_number_key, phone_number_key
            FROM patients
            WHERE id = :Id";

                using var cmd = new OracleCommand(query, conn) { BindByName = true };
                cmd.Parameters.Add(new OracleParameter("Id", OracleDbType.Int32, id, ParameterDirection.Input));

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string? idCipher = reader["id_number"]?.ToString();
                    string? idKey = reader["id_number_key"]?.ToString();
                    string? phoneCipher = reader["phone_number"]?.ToString();
                    string? phoneKey = reader["phone_number_key"]?.ToString();

                    string? decryptedId = null;
                    string? decryptedPhone = null;

                    try
                    {
                        if (!string.IsNullOrEmpty(idCipher) && !string.IsNullOrEmpty(idKey))
                            decryptedId = _hybridService.DecryptHybrid(idCipher, idKey, _hybridService.GetPrivateKeyBase64());

                        if (!string.IsNullOrEmpty(phoneCipher) && !string.IsNullOrEmpty(phoneKey))
                            decryptedPhone = _hybridService.DecryptHybrid(phoneCipher, phoneKey, _hybridService.GetPrivateKeyBase64());
                    }
                    catch
                    {
                        decryptedId = "[Giải mã lỗi]";
                        decryptedPhone = "[Giải mã lỗi]";
                    }

                    model = new Patient
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        FullName = reader["full_name"]?.ToString(),
                        BirthDate = reader["birth_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["birth_date"]),
                        Gender = reader["gender"]?.ToString(),
                        Address = reader["address"] == DBNull.Value ? null : reader["address"].ToString(),
                        IdNumber = decryptedId,
                        PhoneNumber = decryptedPhone,
                        CreatedAt = reader["created_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["created_at"])
                    };
                }
                else
                {
                    return NotFound();
                }
            }
            catch (OracleException ex)
            {
                TempData["Error"] = $"Lỗi truy xuất dữ liệu: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Patient model)
        {
            if (id != model.Id)
            {
                ModelState.AddModelError("", "Id không khớp.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                // Mã hóa lai cho id & phone
                string publicKey = _hybridService.GetPublicKeyBase64();
                var idEncrypted = _hybridService.EncryptHybrid(model.IdNumber ?? "", publicKey);
                var phoneEncrypted = _hybridService.EncryptHybrid(model.PhoneNumber ?? "", publicKey);

                string plsql = @"
                BEGIN
                    UPDATE patients
                    SET full_name = :FullName,
                        birth_date = :BirthDate,
                        gender = :Gender,
                        address = ENCRYPTION_PKG.encrypt_data(:Address),
                        id_number = :IdCipher,
                        phone_number = :PhoneCipher,
                        id_number_key = :IdKey,
                        phone_number_key = :PhoneKey
                    WHERE id = :Id;
                END;";

                using var tx = conn.BeginTransaction();
                try
                {
                    using var cmd = new OracleCommand(plsql, conn)
                    {
                        BindByName = true,
                        Transaction = tx
                    };

                    cmd.Parameters.Add(new OracleParameter("FullName", OracleDbType.Varchar2, model.FullName ?? (object)DBNull.Value, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("BirthDate", OracleDbType.Date, (object?)model.BirthDate ?? DBNull.Value, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("Gender", OracleDbType.Varchar2, (object?)model.Gender ?? DBNull.Value, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("Address", OracleDbType.Varchar2, model.Address ?? (object)DBNull.Value, ParameterDirection.Input));

                    var pIdCipher = new OracleParameter("IdCipher", OracleDbType.Varchar2, idEncrypted.CipherText ?? (object)DBNull.Value, ParameterDirection.Input) { Size = 2000 };
                    cmd.Parameters.Add(pIdCipher);

                    var pPhoneCipher = new OracleParameter("PhoneCipher", OracleDbType.Varchar2, phoneEncrypted.CipherText ?? (object)DBNull.Value, ParameterDirection.Input) { Size = 2000 };
                    cmd.Parameters.Add(pPhoneCipher);

                    var pIdKey = new OracleParameter("IdKey", OracleDbType.Varchar2, idEncrypted.EncryptedKey ?? (object)DBNull.Value, ParameterDirection.Input) { Size = 2000 };
                    cmd.Parameters.Add(pIdKey);

                    var pPhoneKey = new OracleParameter("PhoneKey", OracleDbType.Varchar2, phoneEncrypted.EncryptedKey ?? (object)DBNull.Value, ParameterDirection.Input) { Size = 2000 };
                    cmd.Parameters.Add(pPhoneKey);

                    cmd.Parameters.Add(new OracleParameter("Id", OracleDbType.Int32, id, ParameterDirection.Input));

                    cmd.ExecuteNonQuery();
                    tx.Commit();

                    TempData["Success"] = "Cập nhật bệnh nhân thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (OracleException ex)
                {
                    try { tx.Rollback(); } catch { /* ignore */ }
                    ModelState.AddModelError("", $"Lỗi khi cập nhật bệnh nhân: {ex.Message}");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(model);
            }
        }

        // ------------------ XÓA BỆNH NHÂN (XÁC NHẬN) ------------------

        // POST: /Patient/DeleteConfirmed (xóa trực tiếp từ Index)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                string query = "DELETE FROM patients WHERE id = :id";
                using var cmd = new OracleCommand(query, conn);
                cmd.Parameters.Add(new OracleParameter(":id", id));
                cmd.ExecuteNonQuery();

                TempData["Success"] = "Xóa bệnh nhân thành công!";
            }
            catch (OracleException ex)
            {
                TempData["Error"] = $"Lỗi khi xóa bệnh nhân: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
