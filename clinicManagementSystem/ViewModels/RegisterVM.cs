using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    
        public class RegisterVM
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Full Name is required.")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email address is required.")]
            [EmailAddress(ErrorMessage = "Invalid email address format.")]
            [DataType(DataType.EmailAddress)]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Phone number is required.")]
            [Phone(ErrorMessage = "Invalid phone number.")]
            [DataType(DataType.PhoneNumber)]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
            public string Password { get; set; } = String.Empty;

            [Required(ErrorMessage = "Confirm Password is required.")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = String.Empty;
        }
    }
 
