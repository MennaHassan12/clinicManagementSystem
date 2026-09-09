using clinicManagementSystem.Areas.Admin.ViewModels;
using clinicManagementSystem.Data;
using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminOrSuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var model = new AdminDashboardVM
            {
                // Main statistics
                TotalPatients = await _context.Patients.CountAsync(),
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TotalMedicalFiles = await _context.MedicalFiles.CountAsync(),
                TotalReviews = await _context.Reviews.CountAsync(),

                // Average rating
                AverageRating = await _context.Reviews.AnyAsync()
                    ? await _context.Reviews.AverageAsync(r => (double)r.Rating)
                    : 0,

                // Today's appointments
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate == today),

                TodayConfirmed = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate == today &&
                                     a.Status == AppointmentStatus.Confirmed),

                TodayPending = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate == today &&
                                     a.Status == AppointmentStatus.Pending),

                TodayCompleted = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate == today &&
                                     a.Status == AppointmentStatus.Completed),

                TodayCancelled = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate == today &&
                                     a.Status == AppointmentStatus.Cancelled),

                // Today's appointments list
                TodayAppointmentsList = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.ApplicationUser)
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.ApplicationUser)
                    .Where(a => a.AppointmentDate == today)
                    .OrderBy(a => a.AppointmentTime)
                    .ToListAsync(),

                // Departments
                DepartmentStats = await _context.Departments
                    .Select(d => new DepartmentDashboardItem
                    {
                        DepartmentId = d.DepartmentId,
                        DepartmentName = d.Name,
                        DoctorCount = d.Doctors.Count()
                    })
                    .OrderByDescending(d => d.DoctorCount)
                    .ToListAsync(),

                // Recent reviews
                RecentReviews = await _context.Reviews
                    .Include(r => r.Appointment)
                        .ThenInclude(a => a.Patient)
                            .ThenInclude(p => p.ApplicationUser)
                    .OrderByDescending(r => r.ReviewDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}