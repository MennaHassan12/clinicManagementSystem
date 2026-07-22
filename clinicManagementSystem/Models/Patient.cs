using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ClinicManagementSystem.Models
{
    public enum Gender
    {
        Male,
        Female
    }
    public class Patient
    {
        public int PatientId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = null!;

        public DateTime BirthDate { get; set; }


        [MaxLength(5)]
        public string BloodType { get; set; } = null!;

        public Gender Gender { get; set; }

        [MaxLength(250)]
        public string Address { get; set; } = null!;

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string? EmergencyContactRelation { get; set; }
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}