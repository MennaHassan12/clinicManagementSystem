using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinicManagementSystem.Models
{
    public class Doctor
    
    {
            public int DoctorId { get; set; }

            [Required]
            public string ApplicationUserId { get; set; } = null!;

            public int DepartmentId { get; set; }

            [Column(TypeName = "decimal(10,2)")]
            public decimal ConsultationFee { get; set; }

            public int YearsOfExperience { get; set; }

            public string? Image { get; set; }

            [MaxLength(1000)]
            public string? Bio { get; set; }

            public int LicenseNumber { get; set; }

            public ApplicationUser ApplicationUser { get; set; } = null!;

            public Department Department { get; set; } = null!;

            public ICollection<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();
            public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

            public ICollection<Review> Reviews { get; set; } = new List<Review>();
        }
    }




