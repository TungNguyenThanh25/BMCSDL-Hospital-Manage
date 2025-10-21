using System.ComponentModel.DataAnnotations;

namespace HospitalManage.Models
{
    public class CreateMedicalRecordViewModel
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chẩn đoán.")]
        [Display(Name = "Chẩn đoán")]
        public string Diagnosis { get; set; }

        [Display(Name = "Ghi chú chi tiết")]
        public string DetailedNotes { get; set; }
    }
}