using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    public class ResendEmailConfirmationVM
    {

        public int Id { get; set; }
        [Required]
        [Display(Name = "Email")]
        public string Email  { get; set; } = string.Empty;
    }
}
