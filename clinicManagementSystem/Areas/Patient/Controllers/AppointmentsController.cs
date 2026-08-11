using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services;
using clinicManagementSystem.ViewModels;

namespace clinicManagementSystem.Areas.Patient.Controllers
{
    [Area("Patient")]
    public class AppointmentsController : Controller
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
        private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<clinicManagementSystem.Models.Patient> _patientRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<MedicalRecord> _recordRepo;
        private readonly IEmailSender _emailSender;

        public AppointmentsController(
            IRepository<Appointment> appointmentRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IRepository<DoctorSchedule> scheduleRepo,
            IRepository<clinicManagementSystem.Models.Patient> patientRepo,
            IRepository<Department> departmentRepo,
            IRepository<MedicalRecord> recordRepo,
            IEmailSender emailSender)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _scheduleRepo = scheduleRepo;
            _patientRepo = patientRepo;
            _departmentRepo = departmentRepo;
            _recordRepo = recordRepo;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(int? departmentId, int? patientId)
        {
            return await Doctors(departmentId, patientId);
        }

        public async Task<IActionResult> Doctors(int? departmentId, int? patientId)
        {
            int targetPatientId = patientId.HasValue && patientId.Value > 0 ? patientId.Value : 1;
            ViewBag.PatientId = targetPatientId;

            var doctors = await _doctorRepo.GetAsync(
                expression: d => !departmentId.HasValue || d.DepartmentId == departmentId.Value,
                includes: [d => d.ApplicationUser, d => d.Department]
            );

            ViewBag.Departments = new SelectList(await _departmentRepo.GetAsync(), "DepartmentId", "Name", departmentId);
            return View("Doctors", doctors);
        }

        public async Task<IActionResult> DoctorDetails(int id, int? patientId)
        {
            int targetPatientId = patientId.HasValue && patientId.Value > 0 ? patientId.Value : 1;
            ViewBag.PatientId = targetPatientId;

            var doctor = await _doctorRepo.GetOneAsync(
                expression: d => d.DoctorId == id,
                includes: [d => d.ApplicationUser, d => d.Department, d => d.DoctorSchedules]
            );

            if (doctor == null) return NotFound();
            return View(doctor);
        }

        public async Task<IActionResult> MyAppointments(int? patientId)
        {
            int targetPatientId = patientId.HasValue && patientId.Value > 0 ? patientId.Value : 1;
            ViewBag.PatientId = targetPatientId;

            var appointments = await _appointmentRepo.GetAsync(
                expression: a => a.PatientId == targetPatientId,
                includes: [a => a.Doctor, a => a.Doctor.ApplicationUser, a => a.Schedule]
            );

            return View(appointments.OrderByDescending(a => a.AppointmentDate));
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        [HttpGet]
        public async Task<IActionResult> Book(int? doctorId, int? patientId, int? scheduleId, string? date)
        {
            int targetPatientId = patientId.HasValue && patientId.Value > 0 ? patientId.Value : 1;
            ViewBag.PatientId = targetPatientId;

            clinicManagementSystem.Models.Doctor? doctor = null;
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
                    Selected = doctorId.HasValue && d.DoctorId == doctorId.Value
                }).ToList(),
                AvailableSchedules = availableSchedules
            };

            ViewBag.SelectedDayOfWeek = (int)(selectedSchedule?.DayOfWeek ?? DayOfWeek.Monday);
            ViewBag.TimeSlots = allTimeSlots;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Book(
            PatientBookingVM model,
            string? appointmentDate,
            string? appointmentTime,
            int? patientId,
            string? PatientName,
            string? PatientEmail,
            string? Notes)
        {
            ModelState.Remove("DoctorName");
            ModelState.Remove("Doctors");
            ModelState.Remove("AvailableSchedules");
            ModelState.Remove("AppointmentDate");
            ModelState.Remove("AppointmentTime");

            int currentPatientId = patientId.HasValue && patientId.Value > 0 ? patientId.Value : 1;
            ViewBag.PatientId = currentPatientId;

            try
            {
                var patient = await _patientRepo.GetOneAsync(
                    expression: p => p.PatientId == currentPatientId,
                    includes: [p => p.ApplicationUser]
                );

                var schedule = await _scheduleRepo.GetOneAsync(expression: s => s.DoctorScheduleId == model.DoctorScheduleId);

                var doctor = await _doctorRepo.GetOneAsync(
                    expression: d => d.DoctorId == model.DoctorId,
                    includes: [d => d.ApplicationUser]
                );

                if (patient != null && model.DoctorId > 0 && schedule != null)
                {
                    var rawDate = !string.IsNullOrEmpty(appointmentDate) ? appointmentDate : model.AppointmentDate;
                    var rawTime = !string.IsNullOrEmpty(appointmentTime) ? appointmentTime : model.AppointmentTime;
                    if (string.IsNullOrEmpty(rawDate) || string.IsNullOrEmpty(rawTime))
                    {
                        TempData["error_notification"] = "Please select a valid date and time slot.";
                        return await ReloadBookingView(model, currentPatientId, DateOnly.FromDateTime(DateTime.Now.AddDays(1)));
                    }

                    DateOnly.TryParse(rawDate, out var parsedDate);
                    TimeOnly.TryParse(rawTime, out var parsedTime);

                    if (parsedDate.DayOfWeek != schedule.DayOfWeek)
                    {
                        TempData["error_notification"] = $"The selected date ({parsedDate.DayOfWeek}) does not match the shift day ({schedule.DayOfWeek}).";
                        return await ReloadBookingView(model, currentPatientId, GetNextDateForDayOfWeek(schedule.DayOfWeek));
                    }

                    var doctorAppointments = await _appointmentRepo.GetAsync(a => a.DoctorId == model.DoctorId);
                    string targetDateStr = parsedDate.ToString("yyyy-MM-dd");
                    var activeAppointmentsOnDate = doctorAppointments
                        .Where(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                                    a.AppointmentDate.ToString("yyyy-MM-dd") == targetDateStr)
                        .ToList();

                    if (activeAppointmentsOnDate.Count >= schedule.MaxPatients)
                    {
                        TempData["error_notification"] = "Capacity for this date is full.";
                        return await ReloadBookingView(model, currentPatientId, parsedDate);
                    }

                    bool isSlotTaken = activeAppointmentsOnDate.Any(a => a.AppointmentTime == parsedTime);
                    if (isSlotTaken)
                    {
                        TempData["error_notification"] = "This time slot is already booked for this specific date.";
                        return await ReloadBookingView(model, currentPatientId, parsedDate);
                    }

                    string name = !string.IsNullOrWhiteSpace(PatientName) ? PatientName : model.PatientName;
                    string email = !string.IsNullOrWhiteSpace(PatientEmail) ? PatientEmail : model.PatientEmail;
                    string userNotes = !string.IsNullOrWhiteSpace(Notes) ? Notes : model.Notes;
                    string formattedNotes = string.Empty;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        formattedNotes = $"Patient: {name}";
                        if (!string.IsNullOrWhiteSpace(email)) formattedNotes += $" ({email})";
                        if (!string.IsNullOrWhiteSpace(userNotes)) formattedNotes += $" - Notes: {userNotes}";
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
                            string formattedTime = DateTime.Today.Add(parsedTime.ToTimeSpan()).ToString("hh:mm tt");

                            string subject = "Appointment Confirmation - Clinic Management System";
                            string htmlMessage = $@"
                                <h2>Appointment Confirmation</h2>
                                <p>Dear <b>{(!string.IsNullOrWhiteSpace(name) ? name : patient.ApplicationUser?.FullName ?? "Patient")}</b>,</p>
                                <p>Your appointment has been successfully booked. Details of your appointment are provided below:</p>
                                <ul>
                                    <li><b>Doctor:</b> {doctorName}</li>
                                    <li><b>Date:</b> {parsedDate:dd MMMM, yyyy} ({parsedDate.DayOfWeek})</li>
                                    <li><b>Time:</b> {formattedTime}</li>
                                    <li><b>Status:</b> Pending Confirmation</li>
                                </ul>
                                <p>Thank you for choosing Clinic Management System.</p>";

                            await _emailSender.SendEmailAsync(recipientEmail, subject, htmlMessage);
                            TempData["success_notification"] = $"Appointment booked and confirmation email sent to {recipientEmail}!";
                        }
                        catch (Exception ex)
                        {
                            TempData["error_notification"] = $"Appointment booked, but email failed: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData["success_notification"] = "Appointment booked successfully! (No recipient email found)";
                    }

                    return RedirectToAction(nameof(MyAppointments), new { patientId = patient.PatientId });
                }

                TempData["error_notification"] = $"Failed to book appointment. PatientNull: {patient == null}, DoctorId: {model.DoctorId}, ScheduleNull: {schedule == null}";
            }
            catch (Exception ex)
            {
                TempData["error_notification"] = $"An error occurred while booking the appointment: {ex.Message}";
            }

            return await ReloadBookingView(model, currentPatientId, DateOnly.FromDateTime(DateTime.Now.AddDays(1)));
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

        private async Task<IActionResult> ReloadBookingView(PatientBookingVM model, int patientId, DateOnly date)
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

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, int? patientId)
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

            return RedirectToAction(nameof(MyAppointments), new { patientId = patientId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? patientId)
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

            return RedirectToAction(nameof(MyAppointments), new { patientId = patientId });
        }
    }
}
