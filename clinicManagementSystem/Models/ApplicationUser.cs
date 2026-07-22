using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(30)]
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public string? Image {  get; set; }
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }

    }
}
