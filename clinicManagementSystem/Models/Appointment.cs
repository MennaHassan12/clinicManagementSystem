using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

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

        public int DoctorScheduleId { get; set; }

        public DateOnly AppointmentDate { get; set; }
        public TimeOnly AppointmentTime { get; set; }

        public AppointmentStatus Status { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Doctor Doctor { get; set; } = null!;

        public Patient Patient { get; set; } = null!;

        public DoctorSchedule Schedule { get; set; } = null!;

        public MedicalRecord? MedicalRecord { get; set; }

        public Review? Review { get; set; }
    }

}