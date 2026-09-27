using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
 public class Department
        {
            public int DepartmentId { get; set; }

            [Required(ErrorMessage = "Department Name is required.")]
            [MaxLength(100)]
            public string Name { get; set; } = null!;

            [MaxLength(500)]
            public string? Description { get; set; }

            public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
        }
    }

