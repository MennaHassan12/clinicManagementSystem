using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace ClinicManagementSystem.Models
{
    public enum AppointmentStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public int DoctorId { get; set; }

        public int PatientId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public AppointmentStatus Status { get; set; }

        public Doctor Doctor { get; set; } = null!;

        public Patient Patient { get; set; } = null!;

        public MedicalRecord? MedicalRecord { get; set; }

        public Review? Review { get; set; }
    }
}