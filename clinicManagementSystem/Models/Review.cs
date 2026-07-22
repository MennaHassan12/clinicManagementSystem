using clinicManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        public int AppointmentId { get; set; }

        public int DoctorId { get; set; }

        public int PatientId { get; set; }

        [Range(1, 5)]
        public byte Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; }

        public Appointment Appointment { get; set; } = null!;

        public Doctor Doctor { get; set; } = null!;

        public Patient Patient { get; set; } = null!;
    }
}