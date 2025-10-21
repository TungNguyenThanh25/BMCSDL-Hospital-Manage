using System.ComponentModel.DataAnnotations;

namespace HospitalManage.Models
{
    public class Appointment
    {
        public int id { get; set; } // Mã cuộc hẹn
        
        public int patient_id { get; set; } // Mã bệnh nhân
        
        public int doctor_id { get; set; } // Mã bác sĩ
        public DateTime appointment_date { get; set; } // Ngày hẹn khám

        public string notes { get; set; } // Ghi chú

        public DateTime created_at { get; set; } // Ngày tạo
    }
}
