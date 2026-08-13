using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class ValidateOTPVM
    {
        public int Id { get; set; }
         
        [Required]
        public string Email { get; set; } = string.Empty;
 
        [Required(ErrorMessage = "Please enter the OTP code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
        public string Otp { get; set; } = string.Empty;
    }
}
