using System.ComponentModel.DataAnnotations;

namespace HospitalManage.Models
{
    public class BillingViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã hồ sơ.")]
        [Display(Name = "Mã hồ sơ bệnh án")]
        public int MedicalRecordId { get; set; }

        [Display(Name = "Tổng tiền")]
        public double TotalAmount { get; set; }

        public double DrugFee { get; set; }
        public double ExamFee { get; set; }

        public string EncryptedDrugFee { get; set; }     // AES
        public string EncryptedExamFee { get; set; }     // RSA
        public string EncryptedHybridData { get; set; }  // Hybrid
        public string EncryptedHybridKey { get; set; }   // Hybrid
    }
}
