namespace HospitalManage.Models
{
    public class Doctor
    {
        public long Id { get; set; }
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
