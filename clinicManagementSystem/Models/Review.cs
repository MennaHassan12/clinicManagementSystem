using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select an appointment.")]
        public int AppointmentId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; }

        public Appointment? Appointment { get; set; }
    }
}