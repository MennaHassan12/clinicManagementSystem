using clinicManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class Prescription
    {
        public int PrescriptionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string MedicineName { get; set; } = null!;

        [MaxLength(100)]
        public string? Dosage { get; set; }

        [MaxLength(100)]
        public string? Frequency { get; set; }

        [MaxLength(100)]
        public string? Duration { get; set; }

        [MaxLength(500)]
        public string? Instructions { get; set; }

        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; } = null!;
    }
}