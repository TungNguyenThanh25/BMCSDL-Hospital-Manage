namespace HospitalManage.Models
{
    public class MedicalRecordViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime ExaminationDate { get; set; }
        public string Diagnosis { get; set; }
        public string DetailedNotes { get; set; }
    }
}