using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class LoginVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Email address is required.")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
