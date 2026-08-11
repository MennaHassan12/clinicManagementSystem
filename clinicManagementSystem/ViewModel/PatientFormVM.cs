using System;
using System.ComponentModel.DataAnnotations;
using clinicManagementSystem.Models;

namespace clinicManagementSystem.ViewModels
{
    public class PatientFormVM
    {
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; } = DateTime.Now.AddYears(-20);

        public Gender Gender { get; set; }

        public string? BloodType { get; set; }

        public string? Address { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string? EmergencyContactRelation { get; set; }
    }
}