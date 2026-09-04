using Microsoft.AspNetCore.Mvc;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;
using clinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctorRole")]
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

        [HttpGet]
        public async Task<IActionResult> Index(string? searchString)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account", new { area = SD.IDENTITY_AREA });

            var doctor = await _doctorRepo.GetOneAsync(d => d.ApplicationUserId == userId);
            if (doctor == null)
                return NotFound("Doctor profile not found for the logged-in user.");

            ViewBag.SearchString = searchString;
            ViewBag.DoctorId = doctor.DoctorId;

            var appointments = await _appointmentRepo.GetAsync(
                expression: a => a.DoctorId == doctor.DoctorId,
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
        public async Task<IActionResult> ChangeStatus(int id, AppointmentStatus status)
        {
            try
            {
                var fullAppointment = await _appointmentRepo.GetOneAsync(
                    expression: a => a.AppointmentId == id,
                    includes: [a => a.Patient, a => a.Patient.ApplicationUser, a => a.Doctor, a => a.Doctor.ApplicationUser]
                );

                if (fullAppointment == null)
                {
                    TempData["error_notification"] = "Appointment not found!";
                    return RedirectToAction(nameof(Index));
                }

                fullAppointment.Status = status;
                try { _appointmentRepo.Update(fullAppointment); } catch { }
                await _appointmentRepo.CommitAsync();

                string? targetEmail = null;

                if (!string.IsNullOrWhiteSpace(fullAppointment.Notes))
                {
                    var match = Regex.Match(
                        fullAppointment.Notes,
                        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
                    );

                    if (match.Success)
                    {
                        targetEmail = match.Value;
                    }
                }
                if (string.IsNullOrWhiteSpace(targetEmail))
                {
                    targetEmail = fullAppointment.Patient?.ApplicationUser?.Email;
                }

                if (!string.IsNullOrEmpty(targetEmail))
                {
                    try
                    {
                        var doctorName = fullAppointment.Doctor?.ApplicationUser?.FullName ?? "your doctor";
                        var patientName = fullAppointment.Patient?.ApplicationUser?.FullName ?? "Patient";

                        string emailBody = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                                <h2 style='color: #0d6efd;'>Appointment Status Update</h2>
                                <p>Dear <b>{patientName}</b>,</p>
                                <p>Your appointment status with <b>{doctorName}</b> has been updated to: <b style='color: #198754; font-size: 16px;'>{status}</b>.</p>
                                <p><b>Appointment Date:</b> {fullAppointment.AppointmentDate:dd MMMM, yyyy}</p>
                                <p><b>Appointment Time:</b> {fullAppointment.AppointmentTime}</p>
                                <br/>
                                <p style='color: #6c757d; font-size: 12px;'>Thank you for choosing Clinic Management System.</p>
                            </div>";

                        await _emailSender.SendEmailAsync(targetEmail, $"Appointment Update - {status}", emailBody);
                        TempData["success_notification"] = $"Appointment status updated to {status} and email sent successfully!";
                    }
                    catch
                    {
                        TempData["success_notification"] = $"Appointment status updated to {status}, but email failed to send.";
                    }
                }
                else
                {
                    TempData["success_notification"] = $"Appointment status updated to {status} successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["error_notification"] = $"Error updating status: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
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

            return RedirectToAction(nameof(Index));
        }
    }
}
