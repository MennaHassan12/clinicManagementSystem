using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.ViewModels;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

using PatientModel = clinicManagementSystem.Models.Patient;
using DoctorModel = clinicManagementSystem.Models.Doctor;
using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminOrSuperAdmin")]
    public class AppointmentsController : Controller
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<DoctorModel> _doctorRepo;
        private readonly IRepository<PatientModel> _patientRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<MedicalRecord> _recordRepo;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(
            IRepository<Appointment> appointmentRepo,
            IRepository<DoctorModel> doctorRepo,
            IRepository<PatientModel> patientRepo,
            IRepository<Department> departmentRepo,
            IRepository<DoctorSchedule> scheduleRepo,
            IRepository<MedicalRecord> recordRepo,
            IAppointmentService appointmentService,
            UserManager<ApplicationUser> userManager)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _patientRepo = patientRepo;
            _departmentRepo = departmentRepo;
            _scheduleRepo = scheduleRepo;
            _recordRepo = recordRepo;
            _appointmentService = appointmentService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? doctorId, int? departmentId, AppointmentStatus? status, string? searchString)
        {
            var appointments = await _appointmentRepo.GetAsync(
                includes: [
                    a => a.Doctor,
                    a => a.Doctor.ApplicationUser,
                    a => a.Doctor.Department,
                    a => a.Patient,
                    a => a.Patient.ApplicationUser
                ]
            );

            if (doctorId.HasValue && doctorId > 0)
                appointments = appointments.Where(a => a.DoctorId == doctorId.Value);

            if (departmentId.HasValue && departmentId > 0)
                appointments = appointments.Where(a => a.Doctor != null && a.Doctor.DepartmentId == departmentId.Value);

            if (status.HasValue)
                appointments = appointments.Where(a => a.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim().ToLower();
                appointments = appointments.Where(a =>
                    (a.Patient?.ApplicationUser?.FullName != null && a.Patient.ApplicationUser.FullName.ToLower().Contains(searchString)) ||
                    (a.Doctor?.ApplicationUser?.FullName != null && a.Doctor.ApplicationUser.FullName.ToLower().Contains(searchString)) ||
                    (!string.IsNullOrEmpty(a.Notes) && a.Notes.ToLower().Contains(searchString))
                );
            }

            ViewBag.Doctors = new SelectList(await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]), "DoctorId", "ApplicationUser.FullName", doctorId);
            ViewBag.Departments = new SelectList(await _departmentRepo.GetAsync(), "DepartmentId", "Name", departmentId);

            return View(appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentTime));
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PatientBookingVM();
            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);

            model.Doctors = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.ApplicationUser?.FullName ?? $"Doctor #{d.DoctorId}"
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientBookingVM model)
        {
            ModelState.Remove("DoctorName");
            ModelState.Remove("Doctors");
            ModelState.Remove("AvailableSchedules");

            if (!ModelState.IsValid)
            {
                return await ReloadPatientBookingView(model);
            }

            DateOnly appointmentDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            if (DateOnly.TryParse(model.AppointmentDate, out var parsedDate))
            {
                appointmentDate = parsedDate;
            }

            var schedule = await _scheduleRepo.GetOneAsync(s => s.DoctorScheduleId == model.DoctorScheduleId);
            if (schedule != null)
            {
                if (appointmentDate.DayOfWeek != schedule.DayOfWeek)
                {
                    ModelState.AddModelError("", $"The selected date does not fall on a {schedule.DayOfWeek}. Please pick a date on {schedule.DayOfWeek}.");
                    return await ReloadPatientBookingView(model);
                }

                var existingCount = (await _appointmentRepo.GetAsync(a =>
                    a.DoctorId == model.DoctorId &&
                    a.AppointmentDate == appointmentDate &&
                    (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                )).Count();

                if (schedule.MaxPatients > 0 && existingCount >= schedule.MaxPatients)
                {
                    ModelState.AddModelError("", "Sorry, this doctor has reached the maximum number of appointments for this day.");
                    return await ReloadPatientBookingView(model);
                }
            }

            var existingUser = await _userManager.FindByEmailAsync(model.PatientEmail);
            PatientModel? patient = null;
            string? setPasswordLink = null;

            if (existingUser != null)
            {
                patient = await _patientRepo.GetOneAsync(p => p.ApplicationUserId == existingUser.Id);
            }

            if (patient == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = model.PatientEmail,
                    Email = model.PatientEmail,
                    FullName = model.PatientName,
                    PhoneNumber = model.PatientPhone,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(newUser);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return await ReloadPatientBookingView(model);
                }

                await _userManager.AddToRoleAsync(newUser, "Patient");
                DateOnly.TryParse(model.BirthDate, out var parsedDob);

                patient = new PatientModel
                {
                    ApplicationUserId = newUser.Id,
                    BirthDate = parsedDob.ToDateTime(TimeOnly.MinValue)
                };
                await _patientRepo.CreateAsync(patient);
                await _patientRepo.CommitAsync();

                var token = await _userManager.GeneratePasswordResetTokenAsync(newUser);
                setPasswordLink = Url.Action(
                    action: "ResetPassword",
                    controller: "Account",
                    values: new { area = "Identity", code = token, email = newUser.Email },
                    protocol: Request.Scheme
                );
            }

            TimeOnly appointmentTime = TimeOnly.FromDateTime(DateTime.Now);
            if (TimeOnly.TryParse(model.AppointmentTime, out var parsedTime))
            {
                appointmentTime = parsedTime;
            }

            string formattedNotes = string.Empty;
            if (!string.IsNullOrWhiteSpace(model.PatientName))
            {
                formattedNotes = $"Patient: {model.PatientName}";
                if (!string.IsNullOrWhiteSpace(model.PatientEmail)) formattedNotes += $" | Email: {model.PatientEmail}";
                if (!string.IsNullOrWhiteSpace(model.PatientPhone)) formattedNotes += $" | Phone: {model.PatientPhone}";
                if (!string.IsNullOrWhiteSpace(model.BirthDate)) formattedNotes += $" | DOB: {model.BirthDate}";
                if (!string.IsNullOrWhiteSpace(model.Notes)) formattedNotes += $" | Notes: {model.Notes}";
            }
            else
            {
                formattedNotes = model.Notes ?? string.Empty;
            }

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = model.DoctorId,
                DoctorScheduleId = model.DoctorScheduleId,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                Status = AppointmentStatus.Pending,
                Notes = formattedNotes,
                CreatedAt = DateTime.Now
            };

            await _appointmentRepo.CreateAsync(appointment);
            await _appointmentRepo.CommitAsync();

            var doctor = await _doctorRepo.GetOneAsync(d => d.DoctorId == model.DoctorId, includes: [d => d.ApplicationUser]);
            string doctorName = doctor?.ApplicationUser?.FullName ?? "Selected Doctor";

            try
            {
                await _appointmentService.SendAppointmentConfirmationAsync(
                    model.PatientEmail,
                    model.PatientName,
                    doctorName,
                    appointmentDate,
                    appointmentTime,
                    setPasswordLink
                );
            }
            catch { }

            TempData["success_notification"] = "Appointment booked successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorSchedules(int doctorId)
        {
            var schedules = await _scheduleRepo.GetAsync(s => s.DoctorId == doctorId && s.IsAvailable);
            var result = schedules.Select(s => new
            {
                scheduleId = s.DoctorScheduleId,
                dayOfWeek = s.DayOfWeek.ToString(),
                timeText = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, int scheduleId, string date)
        {
            if (DateOnly.TryParse(date, out var bookingDate))
            {
                var slots = await GetScheduleSlotsAsync(doctorId, scheduleId, bookingDate);
                return Json(slots);
            }
            return Json(new List<SelectListItem>());
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, AppointmentStatus status)
        {
            try
            {
                var appointment = await _appointmentRepo.GetOneAsync(
                    expression: a => a.AppointmentId == id,
                    includes: [
                        a => a.Patient,
                        a => a.Patient.ApplicationUser,
                        a => a.Doctor,
                        a => a.Doctor.ApplicationUser
                    ]
                );

                if (appointment != null)
                {
                    appointment.Status = status;
                    await _appointmentRepo.CommitAsync();
                    string? targetEmail = null;
                    string? targetPatientName = null;

                    if (!string.IsNullOrWhiteSpace(appointment.Notes))
                    {
                        var match = Regex.Match(appointment.Notes, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
                        if (match.Success)
                        {
                            targetEmail = match.Value;
                        }

                        if (appointment.Notes.StartsWith("Patient:"))
                        {
                            var parts = appointment.Notes.Split('-');
                            var namePart = parts[0].Replace("Patient:", "").Trim();

                            if (namePart.Contains('|'))
                            {
                                namePart = namePart.Split('|')[0].Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(namePart))
                            {
                                targetPatientName = namePart;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(targetEmail))
                    {
                        targetEmail = appointment.Patient?.ApplicationUser?.Email;
                    }

                    if (string.IsNullOrWhiteSpace(targetPatientName))
                    {
                        targetPatientName = appointment.Patient?.ApplicationUser?.FullName ?? "Patient";
                    }

                    if (!string.IsNullOrEmpty(targetEmail))
                    {
                        var doctorName = appointment.Doctor?.ApplicationUser?.FullName ?? "your doctor";

                        try
                        {
                            await _appointmentService.SendAppointmentStatusUpdateAsync(
                                targetEmail,
                                targetPatientName,
                                doctorName,
                                appointment.AppointmentDate,
                                status
                            );

                            TempData["success_notification"] = $"Appointment status updated to {status} and email notification sent!";
                        }
                        catch (Exception ex)
                        {
                            TempData["error_notification"] = $"Status updated to {status}, but email sending failed: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData["success_notification"] = $"Status updated to {status}, but patient email was not found.";
                    }
                }
                else
                {
                    TempData["error_notification"] = "Appointment not found!";
                }
            }
            catch (Exception ex)
            {
                TempData["error_notification"] = $"Failed to update appointment status: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var appointment = await _appointmentRepo.GetOneAsync(
                    expression: a => a.AppointmentId == id,
                    includes: [a => a.MedicalRecord]
                );
                if (appointment != null)
                {
                    if (appointment.MedicalRecord != null)
                    {
                        var recordWithPrescriptions = await _recordRepo.GetOneAsync(
                            expression: r => r.MedicalRecordId == appointment.MedicalRecord.MedicalRecordId,
                            includes: [r => r.Prescriptions]
                        );
                        if (recordWithPrescriptions != null)
                        {
                            _recordRepo.Delete(recordWithPrescriptions);
                            await _recordRepo.CommitAsync();
                        }
                    }

                    _appointmentRepo.Delete(appointment);
                    await _appointmentRepo.CommitAsync();

                    TempData["success_notification"] = "Appointment deleted successfully!";
                }
                else
                {
                    TempData["error_notification"] = "Appointment not found!";
                }
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["error_notification"] = "Cannot delete appointment: " + innerMessage;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetScheduleSlotsAsync(int doctorId, int scheduleId, DateOnly bookingDate)
        {
            var slots = new List<SelectListItem>();
            var schedule = await _scheduleRepo.GetOneAsync(s => s.DoctorScheduleId == scheduleId);
            if (schedule == null) return slots;

            if (bookingDate.DayOfWeek != schedule.DayOfWeek)
            {
                slots.Add(new SelectListItem
                {
                    Value = "",
                    Text = $"Selected date must be a {schedule.DayOfWeek}",
                    Disabled = true,
                    Selected = true
                });
                return slots;
            }

            var existingAppointments = await _appointmentRepo.GetAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate == bookingDate &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            );

            if (schedule.MaxPatients > 0 && existingAppointments.Count() >= schedule.MaxPatients)
            {
                slots.Add(new SelectListItem
                {
                    Value = "",
                    Text = "Fully Booked for this day",
                    Disabled = true,
                    Selected = true
                });
                return slots;
            }

            var bookedTimes = existingAppointments.Select(a => a.AppointmentTime).ToList();
            TimeOnly current = TimeOnly.FromTimeSpan(schedule.StartTime);
            TimeOnly end = TimeOnly.FromTimeSpan(schedule.EndTime);

            while (current < end)
            {
                bool isBooked = bookedTimes.Contains(current);
                slots.Add(new SelectListItem
                {
                    Value = current.ToString("HH:mm"),
                    Text = isBooked
                        ? $"{DateTime.Today.Add(current.ToTimeSpan()):hh:mm tt} (Booked)"
                        : DateTime.Today.Add(current.ToTimeSpan()).ToString("hh:mm tt"),
                    Disabled = isBooked
                });

                current = current.AddMinutes(30);
            }

            return slots;
        }

        private async Task<IActionResult> ReloadPatientBookingView(PatientBookingVM model)
        {
            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            model.Doctors = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.ApplicationUser?.FullName ?? $"Doctor #{d.DoctorId}",
                Selected = d.DoctorId == model.DoctorId
            }).ToList();

            return View("Create", model);
        }
    }
}
