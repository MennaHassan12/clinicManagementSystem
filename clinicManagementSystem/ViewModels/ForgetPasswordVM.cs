using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class ForgetPasswordVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }= string.Empty;
    }
}
