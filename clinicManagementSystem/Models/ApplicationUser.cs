using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(30)]
        public required string FirstName { get; set; }
        [MaxLength(30)]
        public required string LastName { get; set; }
        public string? Image {  get; set; }
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }

    }
}
