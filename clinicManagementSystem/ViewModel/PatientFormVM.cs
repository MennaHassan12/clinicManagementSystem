using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;

namespace clinicManagementSystem.ViewModel
{
    public class PatientFormVM
    {
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; } = DateTime.Now.AddYears(-20);

        public Gender Gender { get; set; }

        public string? BloodType { get; set; }

        public string? Address { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string? EmergencyContactRelation { get; set; }

        [Required(ErrorMessage = "Please select a doctor.")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Please select a schedule shift.")]
        public int DoctorScheduleId { get; set; }

        [Required(ErrorMessage = "Appointment Date is required.")]
        [DataType(DataType.Date)]
        public DateOnly AppointmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        [Required(ErrorMessage = "Appointment Time is required.")]
        [DataType(DataType.Time)]
        public TimeOnly AppointmentTime { get; set; } = TimeOnly.FromDateTime(DateTime.Now);

        public string? Notes { get; set; }

        public List<SelectListItem> Doctors { get; set; } = new();
    }
}