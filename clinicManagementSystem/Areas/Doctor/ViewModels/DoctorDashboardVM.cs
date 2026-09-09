using clinicManagementSystem.Models;

namespace clinicManagementSystem.Areas.Doctor.ViewModels
{
    public class DoctorDashboardVM
    {
        public string DoctorName { get; set; } = "Doctor";
        public string DepartmentName { get; set; } = "Department";

        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }

        public int MyPatients { get; set; }

        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        public List<Appointment> TodayAppointmentsList { get; set; } = new();
        public List<Review> RecentReviews { get; set; } = new();
    }
}