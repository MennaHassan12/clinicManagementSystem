using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using clinicManagementSystem.Services.IServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using clinicManagementSystem.Utilities;

using DoctorModel = clinicManagementSystem.Models.Doctor;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Policy = "RequirePatientRole")]
    public class AppointmentsController : Controller
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<DoctorModel> _doctorRepo;
        private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<clinicManagementSystem.Models.Patient> _patientRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<MedicalRecord> _recordRepo;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(
            IRepository<Appointment> appointmentRepo,
            IRepository<DoctorModel> doctorRepo,
            IRepository<DoctorSchedule> scheduleRepo,
            IRepository<clinicManagementSystem.Models.Patient> patientRepo,
            IRepository<Department> departmentRepo,
            IRepository<MedicalRecord> recordRepo,
            IAppointmentService appointmentService,
            UserManager<ApplicationUser> userManager)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _scheduleRepo = scheduleRepo;
            _patientRepo = patientRepo;
            _departmentRepo = departmentRepo;
            _recordRepo = recordRepo;
            _appointmentService = appointmentService;
            _userManager = userManager;
        }

        

        public async Task<IActionResult> DoctorDetails(int id)
        {
            var doctor = await _doctorRepo.GetOneAsync(
                expression: d => d.DoctorId == id,
                includes: [d => d.ApplicationUser, d => d.Department, d => d.DoctorSchedules]
            );

            if (doctor == null) return NotFound();
            return View(doctor);
        }

        public async Task<IActionResult> MyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account", new { area = SD.IDENTITY_AREA });

            var patient = await _patientRepo.GetOneAsync(
                expression: p => p.ApplicationUserId == userId,
                includes: [p => p.ApplicationUser]
            );

            if (patient == null)
            {
                return View(new List<Appointment>());
            }

            var appointments = await _appointmentRepo.GetAsync(
                expression: a => a.PatientId == patient.PatientId,
                includes: [
                    a => a.Doctor,
                    a => a.Doctor.ApplicationUser,
                    a => a.Schedule
                ]
            );
            var resultList = appointments != null
                            ? appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentTime).ToList()
                            : new List<Appointment>();

            return View(resultList);
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        [HttpGet]
        public async Task<IActionResult> Book(int? doctorId, int? scheduleId, string? date)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account", new { area = SD.IDENTITY_AREA });

            var patient = await _patientRepo.GetOneAsync(
                expression: p => p.ApplicationUserId == userId,
                includes: [p => p.ApplicationUser]
            );

            DoctorModel? doctor = null;
            if (doctorId.HasValue && doctorId.Value > 0)
            {
                doctor = await _doctorRepo.GetOneAsync(
                    expression: d => d.DoctorId == doctorId.Value,
                    includes: [d => d.ApplicationUser, d => d.DoctorSchedules]
                );
            }

            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            var availableSchedules = doctor?.DoctorSchedules?.Where(s => s.IsAvailable).ToList() ?? new List<DoctorSchedule>();

            DoctorSchedule? selectedSchedule = null;
            if (scheduleId.HasValue && scheduleId.Value > 0)
            {
                selectedSchedule = availableSchedules.FirstOrDefault(s => s.DoctorScheduleId == scheduleId.Value);
            }

            if (selectedSchedule == null)
            {
                selectedSchedule = availableSchedules.FirstOrDefault();
            }

            DateOnly bookingDate;
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsedDate) && selectedSchedule != null && parsedDate.DayOfWeek == selectedSchedule.DayOfWeek)
            {
                bookingDate = parsedDate;
            }
            else
            {
                bookingDate = GetNextDateForDayOfWeek(selectedSchedule?.DayOfWeek ?? DayOfWeek.Monday);
            }

            List<SelectListItem> allTimeSlots = GetAllScheduleSlots(selectedSchedule);

            var viewModel = new PatientBookingVM
            {
                DoctorId = doctorId ?? 0,
                DoctorName = doctor?.ApplicationUser?.FullName,
                DoctorScheduleId = selectedSchedule?.DoctorScheduleId ?? 0,
                AppointmentDate = bookingDate.ToString("yyyy-MM-dd"),
                Doctors = doctors.Select(d => new SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = d.ApplicationUser?.FullName ?? $"Dr. #{d.DoctorId}",
                    Selected = doctorId.HasValue && d.DoctorId == d.DoctorId
                }).ToList(),
                AvailableSchedules = availableSchedules
            };

            ViewBag.SelectedDayOfWeek = (int)(selectedSchedule?.DayOfWeek ?? DayOfWeek.Monday);
            ViewBag.TimeSlots = allTimeSlots;

            return View(viewModel);
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Book(
    PatientBookingVM model,
    string? appointmentDate,
    string? appointmentTime,
    string? PatientName,
    string? PatientEmail,
    string? PatientPhone,
    string? BirthDate,
    string? Notes)
{
    ModelState.Remove("DoctorName");
    ModelState.Remove("Doctors");
    ModelState.Remove("AvailableSchedules");
    ModelState.Remove("AppointmentDate");
    ModelState.Remove("AppointmentTime");

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return RedirectToAction("Login", "Account", new { area = SD.IDENTITY_AREA });

    try
    {
        var patient = await _patientRepo.GetOneAsync(
            expression: p => p.ApplicationUserId == userId,
            includes: [p => p.ApplicationUser]
        );

        if (patient == null)
        {
            patient = new clinicManagementSystem.Models.Patient
            {
                ApplicationUserId = userId
            };
            await _patientRepo.CreateAsync(patient);
            await _patientRepo.CommitAsync();
        }

        var schedule = await _scheduleRepo.GetOneAsync(expression: s => s.DoctorScheduleId == model.DoctorScheduleId);
        var doctor = await _doctorRepo.GetOneAsync(
            expression: d => d.DoctorId == model.DoctorId,
            includes: [d => d.ApplicationUser]
        );

        if (model.DoctorId > 0 && schedule != null)
        {
            var rawDate = !string.IsNullOrEmpty(appointmentDate) ? appointmentDate : model.AppointmentDate;
            var rawTime = !string.IsNullOrEmpty(appointmentTime) ? appointmentTime : model.AppointmentTime;

            if (string.IsNullOrEmpty(rawDate) || string.IsNullOrEmpty(rawTime))
            {
                TempData["error_notification"] = "Please select a valid date and time slot.";
                return await ReloadBookingView(model, DateOnly.FromDateTime(DateTime.Now.AddDays(1)));
            }

            DateOnly.TryParse(rawDate, out var parsedDate);
            TimeOnly.TryParse(rawTime, out var parsedTime);

            if (parsedDate.DayOfWeek != schedule.DayOfWeek)
            {
                TempData["error_notification"] = $"The selected date ({parsedDate.DayOfWeek}) does not match the shift day ({schedule.DayOfWeek}).";
                return await ReloadBookingView(model, GetNextDateForDayOfWeek(schedule.DayOfWeek));
            }

            var doctorAppointments = await _appointmentRepo.GetAsync(a => a.DoctorId == model.DoctorId);
            var activeAppointmentsOnDate = doctorAppointments
                .Where(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                            a.AppointmentDate == parsedDate)
                .ToList();

            if (activeAppointmentsOnDate.Count >= schedule.MaxPatients)
            {
                TempData["error_notification"] = "Capacity for this date is full.";
                return await ReloadBookingView(model, parsedDate);
            }

            bool isSlotTaken = activeAppointmentsOnDate.Any(a => a.AppointmentTime == parsedTime);
            if (isSlotTaken)
            {
                TempData["error_notification"] = "This time slot is already booked for this specific date.";
                return await ReloadBookingView(model, parsedDate);
            }

            string name = !string.IsNullOrWhiteSpace(PatientName) ? PatientName : model.PatientName;
            string email = !string.IsNullOrWhiteSpace(PatientEmail) ? PatientEmail : model.PatientEmail;
            string phone = !string.IsNullOrWhiteSpace(PatientPhone) ? PatientPhone : model.PatientPhone;
            string birthDate = !string.IsNullOrWhiteSpace(BirthDate) ? BirthDate : model.BirthDate;
            string userNotes = !string.IsNullOrWhiteSpace(Notes) ? Notes : model.Notes;

            string formattedNotes = string.Empty;
            if (!string.IsNullOrWhiteSpace(name))
            {
                formattedNotes = $"Patient: {name}";
                if (!string.IsNullOrWhiteSpace(email)) formattedNotes += $" | Email: {email}";
                if (!string.IsNullOrWhiteSpace(phone)) formattedNotes += $" | Phone: {phone}";
                if (!string.IsNullOrWhiteSpace(birthDate)) formattedNotes += $" | DOB: {birthDate}";
                if (!string.IsNullOrWhiteSpace(userNotes)) formattedNotes += $" | Notes: {userNotes}";
            }
            else
            {
                formattedNotes = userNotes ?? string.Empty;
            }

            var appointment = new Appointment
            {
                DoctorId = model.DoctorId,
                PatientId = patient.PatientId,
                DoctorScheduleId = schedule.DoctorScheduleId,
                AppointmentDate = parsedDate,
                AppointmentTime = parsedTime,
                Notes = formattedNotes,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.Now
            };

            await _appointmentRepo.CreateAsync(appointment);
            await _appointmentRepo.CommitAsync();

            string recipientEmail = !string.IsNullOrWhiteSpace(email) ? email : patient.ApplicationUser?.Email;

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                try
                {
                    string doctorName = doctor?.ApplicationUser?.FullName ?? model.DoctorName ?? "your doctor";
                    string displayPatientName = !string.IsNullOrWhiteSpace(name) ? name : (patient.ApplicationUser?.FullName ?? "Patient");

                    await _appointmentService.SendAppointmentBookingEmailAsync(
                        toEmail: recipientEmail,
                        patientName: displayPatientName,
                        doctorName: doctorName,
                        date: parsedDate,
                        time: parsedTime
                    );

                    TempData["success_notification"] = $"Appointment booked and confirmation email sent to {recipientEmail}!";
                }
                catch (Exception ex)
                {
                    TempData["success_notification"] = $"Appointment booked successfully! (Email notification failed: {ex.Message})";
                }
            }
            else
            {
                TempData["success_notification"] = "Appointment booked successfully!";
            }

            return RedirectToAction(nameof(MyAppointments));
        }

        TempData["error_notification"] = "Failed to book appointment. Please check selected doctor and schedule.";
    }
    catch (Exception ex)
    {
        var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        TempData["error_notification"] = $"An error occurred: {innerMsg}";
    }

    return await ReloadBookingView(model, DateOnly.FromDateTime(DateTime.Now.AddDays(1)));
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var appointment = await _appointmentRepo.GetOneAsync(expression: a => a.AppointmentId == id);
                if (appointment != null)
                {
                    appointment.Status = AppointmentStatus.Cancelled;
                    await _appointmentRepo.CommitAsync();
                    TempData["success_notification"] = "Appointment cancelled successfully!";
                }
            }
            catch (Exception)
            {
                TempData["error_notification"] = "Failed to cancel the appointment.";
            }

            return RedirectToAction(nameof(MyAppointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["error_notification"] = "Cannot delete: " + innerMessage;
            }

            return RedirectToAction(nameof(MyAppointments));
        }

        private List<SelectListItem> GetAllScheduleSlots(DoctorSchedule? schedule)
        {
            var slots = new List<SelectListItem>();
            if (schedule == null) return slots;

            TimeOnly current = TimeOnly.FromTimeSpan(schedule.StartTime);
            TimeOnly end = TimeOnly.FromTimeSpan(schedule.EndTime);

            while (current < end)
            {
                slots.Add(new SelectListItem
                {
                    Value = current.ToString("HH:mm"),
                    Text = DateTime.Today.Add(current.ToTimeSpan()).ToString("hh:mm tt")
                });
                current = current.AddMinutes(30);
            }

            return slots;
        }

        private DateOnly GetNextDateForDayOfWeek(DayOfWeek targetDay)
        {
            DateTime start = DateTime.Now.AddDays(1);
            while (start.DayOfWeek != targetDay)
            {
                start = start.AddDays(1);
            }
            return DateOnly.FromDateTime(start);
        }

        private async Task<IActionResult> ReloadBookingView(PatientBookingVM model, DateOnly date)
        {
            var doctor = await _doctorRepo.GetOneAsync(
                expression: d => d.DoctorId == model.DoctorId,
                includes: [d => d.ApplicationUser, d => d.DoctorSchedules]
            );

            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            var schedule = doctor?.DoctorSchedules?.FirstOrDefault(s => s.DoctorScheduleId == model.DoctorScheduleId);

            ViewBag.SelectedDayOfWeek = (int)(schedule?.DayOfWeek ?? DayOfWeek.Monday);
            ViewBag.TimeSlots = GetAllScheduleSlots(schedule);

            model.DoctorName = doctor?.ApplicationUser?.FullName;
            model.AppointmentDate = date.ToString("yyyy-MM-dd");
            model.AvailableSchedules = doctor?.DoctorSchedules?.Where(s => s.IsAvailable).ToList() ?? new List<DoctorSchedule>();
            model.Doctors = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.ApplicationUser?.FullName ?? $"Dr. #{d.DoctorId}",
                Selected = d.DoctorId == model.DoctorId
            }).ToList();

            return View("Book", model);
        }
    }
}
