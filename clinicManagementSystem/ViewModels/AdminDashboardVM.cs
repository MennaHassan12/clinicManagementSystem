using clinicManagementSystem.Models;

namespace clinicManagementSystem.Areas.Admin.ViewModels
{
    public class AdminDashboardVM
    {
        // Main Statistics
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalMedicalFiles { get; set; }
        public int TotalReviews { get; set; }

        // Reviews
        public double AverageRating { get; set; }

        // Today's Appointments
        public int TodayAppointments { get; set; }
        public int TodayConfirmed { get; set; }
        public int TodayPending { get; set; }
        public int TodayCompleted { get; set; }
        public int TodayCancelled { get; set; }

        // Today's Appointments List
        public List<Appointment> TodayAppointmentsList { get; set; } = new();

        // Departments Statistics
        public List<DepartmentDashboardItem> DepartmentStats { get; set; } = new();

        // Recent Reviews
        public List<Review> RecentReviews { get; set; } = new();
    }

    public class DepartmentDashboardItem
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int DoctorCount { get; set; }
    }
}