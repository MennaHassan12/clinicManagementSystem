using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;

namespace clinicManagementSystem.ViewModels
{
    public class PatientBookingVM
    {
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public int DoctorScheduleId { get; set; }
        [Required(ErrorMessage = "Patient Name is required.")]
        public string PatientName { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string PatientEmail { get; set; }

        
        public string AppointmentDate { get; set; } = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

        public string AppointmentTime { get; set; } = DateTime.Now.ToString("HH:mm");

        public string? Notes { get; set; }

        public List<SelectListItem>? Doctors { get; set; } = new();
        public List<DoctorSchedule>? AvailableSchedules { get; set; } = new();
    }
}