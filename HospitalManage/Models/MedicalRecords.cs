namespace HospitalManage.Models
{
    public class MedicalRecords
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
        public DateTime ExaminationDate { get; set; }
        public string Diagnosis { get; set; }
        public string DetailedNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
