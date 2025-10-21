namespace HospitalManage.Models
{
    public class Prescription
    {
        public long Id { get; set; }
        public long MedicalRecordId { get; set; }
        public long MedicationId { get; set; }
        public long Quantity { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
