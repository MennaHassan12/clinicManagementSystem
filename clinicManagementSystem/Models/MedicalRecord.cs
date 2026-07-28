using clinicManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace clinicManagementSystem.Models
{
    public class MedicalRecord
    {
        public int MedicalRecordId { get; set; }

        public int AppointmentId { get; set; }

        [MaxLength(1000)]
        public string Diagnosis { get; set; } = null!;

    

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public DateTime VisitDate { get; set; }

        public Appointment Appointment { get; set; } = null!;

        public ICollection<MedicalFile> MedicalFiles { get; set; } = new List<MedicalFile>();
        public ICollection<Prescription> Prescriptions {  get; set; } = new List<Prescription>();
    }
}