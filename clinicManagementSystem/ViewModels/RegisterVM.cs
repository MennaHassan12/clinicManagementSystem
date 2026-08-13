using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.ViewModels
{
    
        public class RegisterVM
        {
        public int Id { get; set; }
        [Required]
            public string FullName { get; set; } = string.Empty;


            [Required]
            [DataType(DataType.EmailAddress)]
            public string Email { get; set; } = string.Empty;

        

        [Required]
            [DataType(DataType.PhoneNumber)]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = String.Empty;

            [Required]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = String.Empty;


        }
    }
 
