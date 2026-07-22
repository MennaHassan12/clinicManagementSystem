using ClinicManagementSystem.Models;

namespace clinicManagementSystem.Models
{
    public class DoctorSchedule
    {
        public int DoctorScheduleId { get; set; }

        public int DoctorId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public int MaxPatients { get; set; }

        public bool IsAvailable { get; set; }

        public Doctor? Doctor { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
