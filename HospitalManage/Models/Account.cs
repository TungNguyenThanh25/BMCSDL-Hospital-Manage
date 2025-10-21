namespace HospitalManage.Models
{
    public class Account
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public int? StaffId { get; set; }
    }
}