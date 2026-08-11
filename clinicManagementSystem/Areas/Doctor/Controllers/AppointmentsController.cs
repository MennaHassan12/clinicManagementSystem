using Microsoft.AspNetCore.Mvc;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services;
using System.Text.RegularExpressions;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class AppointmentsController : Controller
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
        private readonly IEmailSender _emailSender;

        public AppointmentsController(
            IRepository<Appointment> appointmentRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IEmailSender emailSender)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(int? doctorId, string? searchString)
        {
            int currentDoctorId = doctorId.HasValue && doctorId.Value > 0 ? doctorId.Value : 1;
            ViewBag.DoctorId = currentDoctorId;
            ViewBag.SearchString = searchString;

            var appointments = await _appointmentRepo.GetAsync(
                expression: a => a.DoctorId == currentDoctorId,
                includes: [a => a.Patient, a => a.Patient.ApplicationUser, a => a.Schedule]
            );

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim().ToLower();
                appointments = appointments.Where(a =>
                    (a.Patient?.ApplicationUser?.FullName != null && a.Patient.ApplicationUser.FullName.ToLower().Contains(searchString)) ||
                    (!string.IsNullOrEmpty(a.Notes) && a.Notes.ToLower().Contains(searchString))
                );
            }

            return View(appointments.OrderByDescending(a => a.AppointmentDate));
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, AppointmentStatus status, int? doctorId)
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

                    string? patientEmail = appointment.Patient?.ApplicationUser?.Email;

                    if (string.IsNullOrWhiteSpace(patientEmail) && !string.IsNullOrWhiteSpace(appointment.Notes))
                    {
                        var match = Regex.Match(appointment.Notes, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
                        if (match.Success)
                        {
                            patientEmail = match.Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        var doctorName = appointment.Doctor?.ApplicationUser?.FullName ?? "your doctor";
                        var patientName = appointment.Patient?.ApplicationUser?.FullName ?? "Patient";
                        string emailBody = $@"
                            <h2>Appointment Status Update</h2>
                            <p>Dear <b>{patientName}</b>,</p>
                            <p>Your appointment status with <b>{doctorName}</b> has been updated to: <b style='color: blue;'>{status}</b>.</p>
                            <p><b>Appointment Date:</b> {appointment.AppointmentDate:dd MMMM, yyyy}</p>
                            <br/>
                            <p>Thank you for choosing Clinic Management System.</p>";

                        await _emailSender.SendEmailAsync(patientEmail, $"Appointment Update - {status}", emailBody);
                        TempData["success_notification"] = $"Appointment status updated to {status} and notification email sent to {patientEmail}!";
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
                TempData["error_notification"] = $"Status updated, but email sending failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { doctorId = doctorId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int? doctorId)
        {
            try
            {
                var appointment = await _appointmentRepo.GetOneAsync(expression: a => a.AppointmentId == id);
                if (appointment != null)
                {
                    _appointmentRepo.Delete(appointment);
                    await _appointmentRepo.CommitAsync();
                    TempData["success_notification"] = "Appointment deleted successfully!";
                }
                else
                {
                    TempData["error_notification"] = "Appointment not found!";
                }
            }
            catch (Exception)
            {
                TempData["error_notification"] = "Cannot delete appointment due to related records.";
            }

            return RedirectToAction(nameof(Index), new { doctorId = doctorId });
        }
    }
}
