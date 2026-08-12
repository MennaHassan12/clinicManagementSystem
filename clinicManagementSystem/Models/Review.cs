using clinicManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        public int AppointmentId { get; set; }

        

        [Range(1, 5)]
        public byte Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; }

        public Appointment? Appointment { get; set; }


    }
}