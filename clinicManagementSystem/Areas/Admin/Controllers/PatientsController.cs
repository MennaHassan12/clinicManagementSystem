using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using PatientModel = clinicManagementSystem.Models.Patient;
using Microsoft.AspNetCore.Identity.UI.Services;
using clinicManagementSystem.ViewModel;

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PatientsController : Controller
    {
        private readonly IRepository<PatientModel> _patientRepo;
        private readonly IRepository<ApplicationUser> _userRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
        private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IEmailSender _emailSender;

        public PatientsController(
            IRepository<PatientModel> patientRepo,
            IRepository<ApplicationUser> userRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IRepository<DoctorSchedule> scheduleRepo,
            IRepository<Appointment> appointmentRepo,
            IEmailSender emailSender)
        {
            _patientRepo = patientRepo;
            _userRepo = userRepo;
            _doctorRepo = doctorRepo;
            _scheduleRepo = scheduleRepo;
            _appointmentRepo = appointmentRepo;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(string? searchName)
        {
            var patients = await _patientRepo.GetAsync(includes: [p => p.ApplicationUser]);

            if (!string.IsNullOrEmpty(searchName))
            {
                patients = patients.Where(p => p.ApplicationUser != null &&
                    p.ApplicationUser.FullName.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return View(patients);
        }

        public async Task<IActionResult> Details(int id)
        {
            var patient = await _patientRepo.GetOneAsync(
                expression: p => p.PatientId == id,
                includes: [p => p.ApplicationUser]
            );

            if (patient == null) return NotFound();

            return View(patient);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var doctors = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);

            var viewModel = new PatientFormVM
            {
                Doctors = doctors.Select(d => new SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = d.ApplicationUser?.FullName ?? $"Doctor #{d.DoctorId}"
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorSchedules(int doctorId)
        {
            var schedules = await _scheduleRepo.GetAsync(s => s.DoctorId == doctorId);

            if (schedules == null || !schedules.Any())
            {
                return Json(new List<object>());
            }

            var result = schedules.Select(s => new
            {
                id = s.DoctorScheduleId,
                text = $"{s.DayOfWeek} ({DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt})"
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int scheduleId)
        {
            var schedule = await _scheduleRepo.GetOneAsync(s => s.DoctorScheduleId == scheduleId);
            if (schedule == null) return Json(new List<object>());

            var slots = new List<object>();
            TimeSpan current = schedule.StartTime;
            TimeSpan end = schedule.EndTime;
            while (current < end)
            {
                var timeOnly = TimeOnly.FromTimeSpan(current);
                var formattedText = DateTime.Today.Add(current).ToString("hh:mm tt");

                slots.Add(new
                {
                    value = timeOnly.ToString("HH:mm"),
                    text = formattedText
                });

                current = current.Add(TimeSpan.FromMinutes(30));
            }

            return Json(slots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientFormVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var isBooked = await _appointmentRepo.GetOneAsync(a =>
                        a.DoctorId == model.DoctorId &&
                        a.AppointmentDate == model.AppointmentDate &&
                        a.AppointmentTime == model.AppointmentTime &&
                        a.Status != AppointmentStatus.Cancelled);

                    if (isBooked != null)
                    {
                        ModelState.AddModelError("", "This time slot is already booked for the selected doctor.");
                        await RepopulateDropdowns(model);
                        return View(model);
                    }

                    var userEmail = string.IsNullOrWhiteSpace(model.Email)
                        ? $"patient_{Guid.NewGuid().ToString().Substring(0, 6)}@clinic.com"
                        : model.Email;

                    var user = new ApplicationUser
                    {
                        UserName = userEmail,
                        Email = userEmail,
                        FullName = model.Name,
                        PhoneNumber = model.Phone
                    };

                    await _userRepo.CreateAsync(user);
                    await _userRepo.CommitAsync();

                    var patient = new PatientModel
                    {
                        ApplicationUserId = user.Id,
                        BirthDate = model.BirthDate,
                        Gender = model.Gender,
                        BloodType = string.IsNullOrWhiteSpace(model.BloodType) ? "N/A" : model.BloodType,
                        Address = string.IsNullOrWhiteSpace(model.Address) ? "N/A" : model.Address,
                        EmergencyContactName = model.EmergencyContactName ?? "",
                        EmergencyContactPhone = model.EmergencyContactPhone ?? "",
                        EmergencyContactRelation = model.EmergencyContactRelation ?? ""
                    };

                    await _patientRepo.CreateAsync(patient);
                    await _patientRepo.CommitAsync();

                    string formattedNotes = $"Patient: {model.Name} (Email: {userEmail}) - Notes: {model.Notes ?? "Admin Direct Booking"}";

                    var appointment = new Appointment
                    {
                        DoctorId = model.DoctorId,
                        PatientId = patient.PatientId,
                        DoctorScheduleId = model.DoctorScheduleId,
                        AppointmentDate = model.AppointmentDate,
                        AppointmentTime = model.AppointmentTime,
                        Status = AppointmentStatus.Confirmed,
                        Notes = formattedNotes,
                        CreatedAt = DateTime.Now
                    };

                    await _appointmentRepo.CreateAsync(appointment);
                    await _appointmentRepo.CommitAsync();

                    if (!string.IsNullOrEmpty(model.Email))
                    {
                        try
                        {
                            var doctor = await _doctorRepo.GetOneAsync(d => d.DoctorId == model.DoctorId, includes: [d => d.ApplicationUser]);
                            string emailBody = $@"
                                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                                    <h2 style='color: #0d6efd;'>Appointment Confirmation</h2>
                                    <p>Dear <b>{model.Name}</b>,</p>
                                    <p>Your appointment has been successfully booked by the administration.</p>
                                    <p><b>Doctor:</b> {doctor?.ApplicationUser?.FullName ?? "Selected Doctor"}</p>
                                    <p><b>Date:</b> {model.AppointmentDate:dd MMMM, yyyy}</p>
                                    <p><b>Time:</b> {DateTime.Today.Add(model.AppointmentTime.ToTimeSpan()):hh:mm tt}</p>
                                    <p><b>Status:</b> Confirmed</p>
                                </div>";

                            await _emailSender.SendEmailAsync(model.Email, "Appointment Confirmation", emailBody);
                        }
                        catch { }
                    }

                    TempData["success_notification"] = "Patient created and appointment confirmed successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Database Error: " + innerMessage);
                }
            }

            await RepopulateDropdowns(model);
            return View(model);
        }

        private async Task RepopulateDropdowns(PatientFormVM model)
        {
            var doctorsList = await _doctorRepo.GetAsync(includes: [d => d.ApplicationUser]);
            model.Doctors = doctorsList.Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.ApplicationUser?.FullName }).ToList();
        }

        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _patientRepo.GetOneAsync(
                expression: p => p.PatientId == id,
                includes: [p => p.ApplicationUser]
            );
            if (patient == null) return NotFound();

            var viewModel = new PatientFormVM
            {
                PatientId = patient.PatientId,
                Name = patient.ApplicationUser?.FullName ?? "",
                Phone = patient.ApplicationUser?.PhoneNumber ?? "",
                Email = patient.ApplicationUser?.Email,
                BirthDate = patient.BirthDate,
                Gender = patient.Gender,
                BloodType = patient.BloodType,
                Address = patient.Address,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactRelation = patient.EmergencyContactRelation
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PatientFormVM model)
        {
            ModelState.Remove("DoctorId");
            ModelState.Remove("DoctorScheduleId");
            ModelState.Remove("AppointmentDate");
            ModelState.Remove("AppointmentTime");

            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _patientRepo.GetOneAsync(
                        expression: p => p.PatientId == model.PatientId,
                        includes: [p => p.ApplicationUser]
                    );

                    if (patient == null) return NotFound();
                    if (patient.ApplicationUser != null)
                    {
                        patient.ApplicationUser.FullName = model.Name;
                        patient.ApplicationUser.PhoneNumber = model.Phone;
                        if (!string.IsNullOrWhiteSpace(model.Email))
                        {
                            patient.ApplicationUser.Email = model.Email;
                        }
                        _userRepo.Update(patient.ApplicationUser);
                        await _userRepo.CommitAsync();
                    }

                    patient.BirthDate = model.BirthDate;
                    patient.Gender = model.Gender;
                    patient.BloodType = model.BloodType ?? "N/A";
                    patient.Address = model.Address ?? "N/A";
                    patient.EmergencyContactName = model.EmergencyContactName;
                    patient.EmergencyContactPhone = model.EmergencyContactPhone;
                    patient.EmergencyContactRelation = model.EmergencyContactRelation;

                    _patientRepo.Update(patient);
                    await _patientRepo.CommitAsync();

                    TempData["success_notification"] = "Patient updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Database Error: " + innerMessage);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var patient = await _patientRepo.GetOneAsync(
                    expression: p => p.PatientId == id,
                    includes: [p => p.ApplicationUser]
                );

                if (patient != null)
                {
                    var appointments = await _appointmentRepo.GetAsync(a => a.PatientId == id);
                    if (appointments != null && appointments.Any())
                    {
                        foreach (var appointment in appointments)
                        {
                            _appointmentRepo.Delete(appointment);
                        }
                        await _appointmentRepo.CommitAsync();
                    }

                    var user = patient.ApplicationUser;

                    _patientRepo.Delete(patient);
                    await _patientRepo.CommitAsync();

                    if (user != null)
                    {
                        _userRepo.Delete(user);
                        await _userRepo.CommitAsync();
                    }

                    TempData["success_notification"] = "Patient, appointments, and account deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["error_notification"] = "Error deleting patient: " + innerMessage;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
