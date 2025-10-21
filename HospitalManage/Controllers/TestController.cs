using HospitalManage.Models;
using HospitalManage.Services;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            bool isConnected = Database.IsConnect();
            ViewBag.Status = isConnected ? "✅ Kết nối thành công!" : "❌ Kết nối thất bại!";
            return View();
        }
        public IActionResult TraCuuCuocHen(int? id)
        {
            List<Appointment> model = new List<Appointment>();
            Database.getConnect();

            string query = "SELECT * FROM appointments where 1 = 1";
            OracleCommand cmd = new OracleCommand(query, Database.conn);

            if (id.HasValue)
            {
                cmd.CommandText += " AND id = :id";
                cmd.Parameters.Add(new OracleParameter("id", id.Value));
            }

            OracleDataAdapter adt = new OracleDataAdapter(cmd);
            DataTable tb = new DataTable();
            adt.Fill(tb);
            Des des = new Des();            
            DesOracle desOracle = new DesOracle(Database.getConnect());

            foreach (DataRow rw in tb.Rows)
            {
                string base64 = rw["notes"].ToString();
                byte[] encryptedBytes;

                try
                {
                    encryptedBytes = Convert.FromBase64String(base64);
                }
                catch (FormatException)
                {
                    Console.WriteLine( "[Lỗi: chuỗi không phải Base64 hợp lệ]");
                    return View();
                }

                if (encryptedBytes.Length % 8 != 0)
                {
                    Console.WriteLine( "[Lỗi: độ dài dữ liệu không chia hết cho 8 → không hợp lệ với DES]");
                    return View();
                }

                model.Add(new Appointment()
                {
                    id = Convert.ToInt32(rw["id"]),

                    patient_id = Convert.ToInt32(rw["patient_id"]),

                    doctor_id = Convert.ToInt32(rw["doctor_id"]),
                    appointment_date = (DateTime)rw["appointment_date"],                    

                    notes = desOracle.DecryptDES( rw["notes"].ToString(), "12345678"),

                    created_at = (DateTime)rw["created_at"]
                });
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CapNhatCuocHen(int? id)
        {
            Appointment model;
            Database.getConnect();

            string query = "SELECT * FROM appointments where 1 = 1";
            OracleCommand cmd = new OracleCommand(query, Database.conn);

            if (id.HasValue)
            {
                cmd.CommandText += " AND id = :id";
                cmd.Parameters.Add(new OracleParameter("id", id.Value));
            }

            OracleDataAdapter adt = new OracleDataAdapter(cmd);
            DataTable tb = new DataTable();
            adt.Fill(tb);
            Des des = new Des();
            DesOracle desOracle = new DesOracle(Database.getConnect());
            if (tb.Rows.Count == 0)
            {
                return RedirectToAction("CapNhat");
            }
            model = (new Appointment()
            {

                id = Convert.ToInt32(tb.Rows[0]["id"]),

                patient_id = Convert.ToInt32(tb.Rows[0]["patient_id"]),

                doctor_id = Convert.ToInt32(tb.Rows[0]["doctor_id"]),
                appointment_date = (DateTime)tb.Rows[0]["appointment_date"],
                created_at = (DateTime)tb.Rows[0]["created_at"],
                notes = desOracle.DecryptDES(tb.Rows[0]["notes"].ToString(), "12345678"),
            });
            return View(model);
        }

        [HttpPost]
        public IActionResult CapNhatCuocHen(Appointment model)
        {
            Database.getConnect();
            Des des = new Des();
            DesOracle desOracle =new DesOracle(Database.getConnect());

            string query = @"UPDATE appointments SET 
                    patient_id = :patient_id,
                    doctor_id = :doctor_id,
                    appointment_date = :appointment_date,
                    notes = :notes,
                    created_at = :created_at
                    WHERE id = :id";

            OracleCommand cmd = new OracleCommand(query, Database.conn);
            cmd.Parameters.Add(new OracleParameter("patient_id", model.patient_id));
            cmd.Parameters.Add(new OracleParameter("doctor_id", model.doctor_id));
            cmd.Parameters.Add(new OracleParameter("appointment_date", model.appointment_date));
            cmd.Parameters.Add(new OracleParameter("notes", desOracle.Encrypt(model.notes, "12345678")));

            cmd.Parameters.Add(new OracleParameter("created_at", model.created_at));
            cmd.Parameters.Add(new OracleParameter("id", model.id));

            cmd.ExecuteNonQuery();
            TempData["Success"] = "Cập nhật cuộc hẹn thành công!";
            return RedirectToAction("TraCuuCuocHen");
        }


        [HttpGet]
        public IActionResult ThemDuLieuCuocHen()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ThemDuLieuCuocHen(Appointment model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            try
            {
                Database.getConnect();
                Des des = new Des();
                string query = @"
            INSERT INTO appointments 
            (id, patient_id, doctor_id, appointment_date, notes, created_at) 
            VALUES (:id, :patient_id, :doctor_id, :appointment_date, :notes, :created_at)";
                DesOracle desOracle = new DesOracle(Database.getConnect());

                OracleCommand cmd = new OracleCommand(query, Database.conn);
                cmd.Parameters.Add(new OracleParameter("id", model.id));
                cmd.Parameters.Add(new OracleParameter("patient_id", model.patient_id));
                cmd.Parameters.Add(new OracleParameter("doctor_id", model.doctor_id));
                cmd.Parameters.Add(new OracleParameter("appointment_date", model.appointment_date));
                cmd.Parameters.Add(new OracleParameter("notes", desOracle.Encrypt(model.notes, "12345678")));
                cmd.Parameters.Add(new OracleParameter("created_at", DateTime.Now));

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Thêm cuộc hẹn thành công!";
                return RedirectToAction("TraCuuCuocHen", "Test");
            }
            catch (OracleException ex)
            {
                TempData["Error"] = "Lỗi Oracle: " + ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                return View(model);
            }
        }

        public IActionResult TraCuuBenhNhan(string gender, string phone)
        {
            HybridOracle hybridOracle = new HybridOracle(Database.getConnect());
            RSAOracle rsaOracle = new RSAOracle(Database.getConnect());
            List<Patient> list = new List<Patient>();
            Database.getConnect();

            string query = "SELECT * FROM patients WHERE 1=1";
            OracleCommand cmd = new OracleCommand(query, Database.conn);

            string desKey = Encoding.UTF8.GetString(Hybrid.GetFixedDESKey()); // DES cứng
            string rsaPubKey = Encoding.UTF8.GetString(RSAKeyProvider.GetPublicKeyBytes());
            string rsaPriKey = Encoding.UTF8.GetString(RSAKeyProvider.GetPrivateKeyBytes());

            // 🔐 Mã hóa gender bằng HybridOracle với khóa DES cứng
            if (!string.IsNullOrEmpty(gender))
            {
                string encryptedGender = hybridOracle.EncryptHybrid(gender, desKey, rsaPubKey);
                cmd.CommandText += " AND DBMS_LOB.SUBSTR(gender, 10, 1) = :gender";
                cmd.Parameters.Add(new OracleParameter("gender", encryptedGender));
            }

            // 🔐 Mã hóa phone bằng RSA
            if (!string.IsNullOrEmpty(phone))
            {
                string encryptedPhone = rsaOracle.Encrypt(phone, rsaPubKey);
                cmd.CommandText += " AND DBMS_LOB.SUBSTR(phone_number, 20, 1) LIKE :phone";
                cmd.Parameters.Add(new OracleParameter("phone", "%" + encryptedPhone + "%"));
            }

            OracleDataAdapter adt = new OracleDataAdapter(cmd);
            DataTable tb = new DataTable();
            adt.Fill(tb);

            foreach (DataRow row in tb.Rows)
            {
                string encryptedGender = row["gender"].ToString();
                string decryptedGender = hybridOracle.DecryptHybrid(encryptedGender, desKey, rsaPriKey);

                list.Add(new Patient()
                {
                    Id = Convert.ToInt32(row["id"]),
                    FullName = row["full_name"].ToString(),
                    BirthDate = (DateTime)row["birth_date"],
                    Gender = decryptedGender,
                    IdNumber = row["id_number"].ToString(),
                    Address = row["address"].ToString(),
                    PhoneNumber = rsaOracle.Decrypt(row["phone_number"].ToString(), rsaPriKey),
                    CreatedAt = (DateTime)row["created_at"]
                });
            }

            return View(list);
        }


        [HttpGet]
        public IActionResult CapNhatBenhNhan(int? id)
        {
            HybridOracle hybridOracle = new HybridOracle(Database.getConnect());
            RSAOracle rsaOracle = new RSAOracle(Database.getConnect());
            Patient model;

            string desKey = Encoding.UTF8.GetString(Hybrid.GetFixedDESKey());
            string rsaPriKey = Encoding.UTF8.GetString(RSAKeyProvider.GetPrivateKeyBytes());

            string query = "SELECT * FROM patients WHERE 1 = 1";
            OracleCommand cmd = new OracleCommand(query, Database.getConnect());

            if (id.HasValue)
            {
                cmd.CommandText += " AND id = :id";
                cmd.Parameters.Add(new OracleParameter("id", id.Value));
            }

            OracleDataAdapter adt = new OracleDataAdapter(cmd);
            DataTable tb = new DataTable();
            adt.Fill(tb);

            if (tb.Rows.Count == 0)
            {
                return RedirectToAction("CapNhatBenhNhan");
            }

            string encryptedGender = tb.Rows[0]["gender"].ToString();
            string decryptedGender = hybridOracle.DecryptHybrid(encryptedGender, desKey, rsaPriKey);

            string encryptedPhone = tb.Rows[0]["phone_number"].ToString();
            string decryptedPhone = rsaOracle.Decrypt(encryptedPhone, rsaPriKey);

            model = new Patient()
            {
                Id = Convert.ToInt32(tb.Rows[0]["id"]),
                FullName = tb.Rows[0]["full_name"].ToString(),
                BirthDate = (DateTime)tb.Rows[0]["birth_date"],
                Gender = decryptedGender,
                IdNumber = tb.Rows[0]["id_number"].ToString(),
                Address = tb.Rows[0]["address"].ToString(),
                PhoneNumber = decryptedPhone,
                CreatedAt = (DateTime)tb.Rows[0]["created_at"]
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult CapNhatBenhNhan(Patient model)
        {
            HybridOracle hybridOracle = new HybridOracle(Database.getConnect());
            RSAOracle rsaOracle = new RSAOracle(Database.getConnect());

            string desKey = Encoding.UTF8.GetString(Hybrid.GetFixedDESKey());
            string rsaPubKey = Encoding.UTF8.GetString(RSAKeyProvider.GetPublicKeyBytes());

            try
            {
                Database.getConnect();

                string query = @"UPDATE patients SET
                            full_name = :full_name,
                            birth_date = :birth_date,
                            gender = :gender,
                            id_number = :id_number,
                            address = :address,
                            phone_number = :phone_number
                        WHERE id = :id";

                OracleCommand cmd = new OracleCommand(query, Database.conn);
                cmd.Parameters.Add(new OracleParameter("full_name", model.FullName));
                cmd.Parameters.Add(new OracleParameter("birth_date", model.BirthDate));
                cmd.Parameters.Add(new OracleParameter("gender", hybridOracle.EncryptHybrid(model.Gender, desKey, rsaPubKey)));
                cmd.Parameters.Add(new OracleParameter("id_number", model.IdNumber));
                cmd.Parameters.Add(new OracleParameter("address", model.Address));
                cmd.Parameters.Add(new OracleParameter("phone_number", rsaOracle.Encrypt(model.PhoneNumber, rsaPubKey)));
                cmd.Parameters.Add(new OracleParameter("id", model.Id));

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Cập nhật thông tin bệnh nhân thành công!";
                return RedirectToAction("TraCuuBenhNhan");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return View(model);
            }
        }



        [HttpGet]
        public IActionResult ThemBenhNhan()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ThemBenhNhan(Patient model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return View(model);
            }

            HybridOracle hybridOracle = new HybridOracle(Database.getConnect());
            RSAOracle rsaOracle = new RSAOracle(Database.getConnect());

            string desKey = Encoding.UTF8.GetString(Hybrid.GetFixedDESKey());
            string rsaPubKey = Encoding.UTF8.GetString(RSAKeyProvider.GetPublicKeyBytes());

            try
            {
                Database.getConnect();

                string query = @"INSERT INTO patients 
                        (id, full_name, birth_date, gender, id_number, address, phone_number, created_at) 
                        VALUES 
                        (:id, :full_name, :birth_date, :gender, :id_number, :address, :phone_number, :created_at)";

                OracleCommand cmd = new OracleCommand(query, Database.conn);
                cmd.Parameters.Add(new OracleParameter("id", model.Id));
                cmd.Parameters.Add(new OracleParameter("full_name", model.FullName));
                cmd.Parameters.Add(new OracleParameter("birth_date", model.BirthDate));
                cmd.Parameters.Add(new OracleParameter("gender", hybridOracle.EncryptHybrid(model.Gender, desKey, rsaPubKey)));
                cmd.Parameters.Add(new OracleParameter("id_number", model.IdNumber));
                cmd.Parameters.Add(new OracleParameter("address", model.Address));
                cmd.Parameters.Add(new OracleParameter("phone_number", rsaOracle.Encrypt(model.PhoneNumber, rsaPubKey)));
                cmd.Parameters.Add(new OracleParameter("created_at", DateTime.Now));

                cmd.ExecuteNonQuery();

                TempData["Success"] = "✅ Thêm bệnh nhân thành công!";
                return RedirectToAction("TraCuuBenhNhan", "Test");
            }
            catch (OracleException ex)
            {
                TempData["Error"] = "❌ Lỗi Oracle: " + ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Lỗi hệ thống: " + ex.Message;
                return View(model);
            }
        }
    }
}
