using clinicManagementSystem.Areas.Doctor.ViewModels;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctorRole")]
    public class DashboardController : Controller
    {
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo; private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<Review> _reviewRepo;

        public DashboardController(
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IRepository<Appointment> appointmentRepo,
            IRepository<Review> reviewRepo)
        {
            _doctorRepo = doctorRepo;
            _appointmentRepo = appointmentRepo;
            _reviewRepo = reviewRepo;
        }

        public async Task<IActionResult> Index()
        {
            // Get the currently logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new
                {
                    area = "Identity"
                });
            }

            // Get the doctor profile for the logged-in user
            var doctor = await _doctorRepo.GetOneAsync(
                d => d.ApplicationUserId == userId,
                includes:
                [
                    d => d.ApplicationUser,
                    d => d.Department
                ]
            );

            if (doctor == null)
            {
                return NotFound("Doctor profile not found for the logged-in user.");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Get this doctor's appointments
            var appointments = await _appointmentRepo.GetAsync(
                expression: a => a.DoctorId == doctor.DoctorId,
                includes:
                [
                    a => a.Patient,
                    a => a.Patient.ApplicationUser,
                    a => a.Schedule
                ],
                tracked: false
            );

            var appointmentList = appointments.ToList();

            // Get this doctor's reviews
            var reviews = await _reviewRepo.GetAsync(
                expression: r => r.Appointment!.DoctorId == doctor.DoctorId,
                includes:
                [
                    r => r.Appointment!,
                    r => r.Appointment.Patient,
                    r => r.Appointment.Patient.ApplicationUser
                ],
                orderBy: q => q.OrderByDescending(r => r.ReviewDate),
                tracked: false
            );

            var reviewList = reviews.ToList();

            // Build dashboard model
            var model = new DoctorDashboardVM
            {
                DoctorName = doctor.ApplicationUser?.FullName ?? "Doctor",
                DepartmentName = doctor.Department?.Name ?? "Department",

                // Appointments
                TotalAppointments = appointmentList.Count(),

                TodayAppointments = appointmentList.Count(a =>
                    a.AppointmentDate == today),

                PendingAppointments = appointmentList.Count(a =>
                    a.Status == AppointmentStatus.Pending),

                ConfirmedAppointments = appointmentList.Count(a =>
                    a.Status == AppointmentStatus.Confirmed),

                CompletedAppointments = appointmentList.Count(a =>
                    a.Status == AppointmentStatus.Completed),

                CancelledAppointments = appointmentList.Count(a =>
                    a.Status == AppointmentStatus.Cancelled),

                // Unique patients
                MyPatients = appointmentList
                    .Select(a => a.PatientId)
                    .Distinct()
                    .Count(),

                // Reviews
                TotalReviews = reviewList.Count(),

                AverageRating = reviewList.Any()
                    ? reviewList.Average(r => (double)r.Rating)
                    : 0,

                // Today's appointments
                TodayAppointmentsList = appointmentList
                    .Where(a => a.AppointmentDate == today)
                    .OrderBy(a => a.AppointmentTime)
                    .ToList(),

                // Recent reviews
                RecentReviews = reviewList
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
    }
}