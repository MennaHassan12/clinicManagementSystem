namespace clinicManagementSystem.Models
{
    public class ApplicationUserOTP
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public string OTP { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;
        public DateTime ValidTo { get; set; } = DateTime.Now.AddMinutes(20);

        public bool IsUsed { get; set; }
    }
}
