using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class ResendEmailConfirmationVM
    {

        public int Id { get; set; }
        [Required(ErrorMessage = "Email address is required.")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email  { get; set; } = string.Empty;
    }
}
