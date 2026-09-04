using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace clinicManagementSystem.Models
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


        
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    }
}