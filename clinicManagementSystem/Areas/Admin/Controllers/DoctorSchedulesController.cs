using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DoctorModel = clinicManagementSystem.Models.Doctor;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminOrSuperAdmin")]
    public class DoctorSchedulesController : Controller
    {
        private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<DoctorModel> _doctorRepo;

        public DoctorSchedulesController(
            IRepository<DoctorSchedule> scheduleRepo,
            IRepository<DoctorModel> doctorRepo)
        {
            _scheduleRepo = scheduleRepo;
            _doctorRepo = doctorRepo;
        }

        public async Task<IActionResult> Index(int? doctorId)
        {
            var schedules = await _scheduleRepo.GetAsync(
                includes: [s => s.Doctor, s => s.Doctor.ApplicationUser]
            );

            if (doctorId.HasValue && doctorId.Value > 0)
            {
                schedules = schedules.Where(s => s.DoctorId == doctorId.Value).ToList();
            }

            var groupedSchedules = schedules
                .GroupBy(s => s.Doctor)
                .Where(g => g.Key != null)
                .ToList();

            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            ViewBag.Doctors = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.ApplicationUser?.FullName ?? $"Doctor #{d.DoctorId}",
                Selected = doctorId == d.DoctorId
            });

            return View(groupedSchedules);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new DoctorScheduleFormVM
            {
                Doctors = await GetDoctorsSelectListAsync()  //Method in the end of the code
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorScheduleFormVM model)
        {
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "End time must be greater than start time.");
            }

            var existingSchedule = await _scheduleRepo.GetAsync(
                s => s.DoctorId == model.DoctorId && s.DayOfWeek == model.DayOfWeek
            );

            if (existingSchedule.Any())
            {
                ModelState.AddModelError("DayOfWeek", "This doctor already has a schedule assigned for this day.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var schedule = new DoctorSchedule
                    {
                        DoctorId = model.DoctorId,
                        DayOfWeek = model.DayOfWeek,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        MaxPatients = model.MaxPatients,
                        IsAvailable = model.IsAvailable
                    };

                    await _scheduleRepo.CreateAsync(schedule);
                    await _scheduleRepo.CommitAsync();

                    TempData["success_notification"] = "Schedule added successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["error_notification"] = "Failed to add schedule. Please try again.";
                }
            }
            else
            {
                TempData["error_notification"] = "Please fix validation errors and try again.";
            }
            model.Doctors = await GetDoctorsSelectListAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _scheduleRepo.GetOneAsync(expression: s => s.DoctorScheduleId == id);
            if (schedule == null) return NotFound();

            var viewModel = new DoctorScheduleFormVM
            {
                DoctorScheduleId = schedule.DoctorScheduleId,
                DoctorId = schedule.DoctorId,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                MaxPatients = schedule.MaxPatients,
                IsAvailable = schedule.IsAvailable,
                Doctors = await GetDoctorsSelectListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DoctorScheduleFormVM model)
        {
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "End time must be greater than start time.");
            }

            var duplicateCheck = await _scheduleRepo.GetAsync(
                s => s.DoctorId == model.DoctorId && s.DayOfWeek == model.DayOfWeek && s.DoctorScheduleId != model.DoctorScheduleId
            );

            if (duplicateCheck.Any())
            {
                ModelState.AddModelError("DayOfWeek", "This doctor already has a schedule assigned for this day.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var schedule = await _scheduleRepo.GetOneAsync(expression: s => s.DoctorScheduleId == model.DoctorScheduleId);
                    if (schedule == null) return NotFound();

                    schedule.DoctorId = model.DoctorId;
                    schedule.DayOfWeek = model.DayOfWeek;
                    schedule.StartTime = model.StartTime;
                    schedule.EndTime = model.EndTime;
                    schedule.MaxPatients = model.MaxPatients;
                    schedule.IsAvailable = model.IsAvailable;

                    _scheduleRepo.Update(schedule);
                    await _scheduleRepo.CommitAsync();

                    TempData["success_notification"] = "Schedule updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["error_notification"] = "Failed to update schedule!";
                }
            }
            else
            {
                TempData["error_notification"] = "Validation failed. Please check inputs.";
            }

            model.Doctors = await GetDoctorsSelectListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var schedule = await _scheduleRepo.GetOneAsync(expression: s => s.DoctorScheduleId == id);
                if (schedule != null)
                {
                    _scheduleRepo.Delete(schedule);
                    await _scheduleRepo.CommitAsync();

                    TempData["success_notification"] = "Schedule deleted successfully!";
                }
                else
                {
                    TempData["error_notification"] = "Schedule not found!";
                }
            }
            catch (Exception)
            {
                TempData["error_notification"] = "Cannot delete schedule as it might be referenced elsewhere.";
            }

            return RedirectToAction(nameof(Index));
        }
        private async Task<IEnumerable<SelectListItem>> GetDoctorsSelectListAsync()
        {
            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            return doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.ApplicationUser?.FullName ?? $"Doctor #{d.DoctorId}"
            });
        }
    }
}
