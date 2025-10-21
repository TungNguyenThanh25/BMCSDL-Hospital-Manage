using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManage.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; } 

        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [Display(Name = "CMND/CCCD")]
        public string? IdNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; }
    }

}
