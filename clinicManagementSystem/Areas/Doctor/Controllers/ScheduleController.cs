using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DoctorModel = clinicManagementSystem.Models.Doctor;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctorRole")]
    public class ScheduleController : Controller
    {
        private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<DoctorModel> _doctorRepo;

        public ScheduleController(
            IRepository<DoctorSchedule> scheduleRepo,
            IRepository<DoctorModel> doctorRepo)
        {
            _scheduleRepo = scheduleRepo;
            _doctorRepo = doctorRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var doctor = await _doctorRepo.GetOneAsync(
                expression: d => d.ApplicationUserId == userId,
                includes: [d => d.ApplicationUser]
            );

            if (doctor is null)
                return NotFound("Doctor profile not found for the logged-in user.");

            var schedules = await _scheduleRepo.GetAsync(
                expression: s => s.DoctorId == doctor.DoctorId
            );

            var ordered = schedules
                .OrderBy(s => (int)s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToList();

            ViewBag.DoctorName = doctor.ApplicationUser?.FullName ?? "Doctor";

            return View(ordered);
        }
    }
}