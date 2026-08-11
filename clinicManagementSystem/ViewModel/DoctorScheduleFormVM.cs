using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace clinicManagementSystem.ViewModels
{
    public class DoctorScheduleFormVM
    {
        public int DoctorScheduleId { get; set; }

        [Required(ErrorMessage = "Please select a doctor.")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Please select a day of the week.")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required(ErrorMessage = "Please specify the start time.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Please specify the end time.")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Please specify max patients capacity.")]
        [Range(1, 100, ErrorMessage = "Max patients must be a positive number between 1 and 100.")]
        public int MaxPatients { get; set; } = 10;

        public bool IsAvailable { get; set; } = true;

        public IEnumerable<SelectListItem>? Doctors { get; set; }
    }
}