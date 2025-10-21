using HospitalManage.Models;
using HospitalManage.Services;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace HospitalManage.Controllers
{
    public class BillingController : Controller
    {
        private readonly EncryptionService _encrypt;
        private readonly BillingService _billing;
        private readonly string _connectionString;

        public BillingController(IConfiguration config)
        {
            HospitalManage.Encryp.RsaKeyStore.Initialize(); // ✅ Khởi tạo RSA key
            _encrypt = new EncryptionService();                   // ✅ Gọi mã hóa & giải mã
            _billing = new BillingService(config);                // ✅ Gọi nghiệp vụ lưu hóa đơn
            _connectionString = config.GetConnectionString("OracleDBConnection");
        }

        // ✅ Kiểm tra kết nối Oracle
        public IActionResult TestConnection()
        {
            try
            {
                using (var conn = new OracleConnection(_connectionString))
                {
                    conn.Open();
                    return Content("✅ Kết nối Oracle thành công");
                }
            }
            catch (Exception ex)
            {
                return Content("❌ Lỗi kết nối Oracle: " + ex.Message);
            }
        }

        // ✅ Hiển thị form nhập mã hồ sơ
        [HttpGet]
        public IActionResult Index()
        {
            return View(new BillingViewModel());
        }

        // ✅ Xử lý nghiệp vụ thanh toán và mã hóa
        [HttpPost]
        public IActionResult Process(BillingViewModel model)
        {
            try
            {
                int recordId = model.MedicalRecordId;

                // 🔍 Tính chi phí
                double examFee = 50000; // Phí khám cố định
                double drugFee = _billing.CalculateDrugFee(recordId);
                double total = examFee + drugFee;

                // 🔐 Mã hóa từng phần
                string encryptedDrugFee = _encrypt.EncryptAES(drugFee.ToString());
                string encryptedExamFee = _encrypt.EncryptRSA(examFee.ToString());
                var hybrid = _encrypt.EncryptHybrid(total.ToString());

                // 💾 Tạo hóa đơn
                var bill = new BillModel
                {
                    MedicalRecordId = recordId,
                    DrugFee = drugFee,
                    ExamFee = examFee,
                    TotalAmount = total,
                    EncryptedDrugFee = encryptedDrugFee,
                    EncryptedExamFee = encryptedExamFee,
                    EncryptedData = hybrid.EncryptedData,
                    EncryptedKey = hybrid.EncryptedKey
                };

                _billing.SaveBill(bill, out string msg);

                // Gán vào model để hiển thị
                model.ExamFee = examFee;
                model.DrugFee = drugFee;
                model.TotalAmount = total;
                model.EncryptedDrugFee = encryptedDrugFee;
                model.EncryptedExamFee = encryptedExamFee;
                model.EncryptedHybridData = hybrid.EncryptedData;
                model.EncryptedHybridKey = hybrid.EncryptedKey;

                ViewBag.Message = msg;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"❌ Lỗi xử lý: {ex.Message}";
            }

            return View("Index", model);
        }

        // ✅ Giải mã hóa đơn theo mã hồ sơ
        public IActionResult DecryptBill(int recordId)
        {
            try
            {
                var bill = _billing.GetBillByRecordId(recordId);
                if (string.IsNullOrEmpty(bill.EncryptedDrugFee) ||
                    string.IsNullOrEmpty(bill.EncryptedExamFee) ||
                    string.IsNullOrEmpty(bill.EncryptedData) ||
                    string.IsNullOrEmpty(bill.EncryptedKey))
                {
                    ViewBag.Message = "❌ Không tìm thấy hóa đơn.";
                    return View("DecryptBill", new BillingViewModel());
                }

                // 🔓 Giải mã từng phần
                string decryptedDrugFee = _encrypt.DecryptAES(bill.EncryptedDrugFee);
                string decryptedExamFee = _encrypt.DecryptRSA(bill.EncryptedExamFee);
                string decryptedTotal = _encrypt.DecryptHybrid(bill.EncryptedData, bill.EncryptedKey);

                var model = new BillingViewModel
                {
                    MedicalRecordId = recordId,
                    DrugFee = double.Parse(decryptedDrugFee),
                    ExamFee = double.Parse(decryptedExamFee),
                    TotalAmount = double.Parse(decryptedTotal)
                };

                ViewBag.Message = "✅ Đã giải mã thành công.";
                return View("DecryptBill", model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"❌ Lỗi giải mã: {ex.Message}";
                return View("DecryptBill", new BillingViewModel());
            }
        }
    }
}
