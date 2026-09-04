using Microsoft.AspNetCore.Mvc;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services.IServices;
using System.Security.Claims;
using clinicManagementSystem.Utilities;
using System.Text.RegularExpressions;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctorRole")]
    public class AppointmentsController : Controller
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(
            IRepository<Appointment> appointmentRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IAppointmentService appointmentService)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _appointmentService = appointmentService;
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
                _appointmentRepo.Update(fullAppointment);
                await _appointmentRepo.CommitAsync();

                string? targetEmail = null;
                string? targetPatientName = null;

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
                    if (fullAppointment.Notes.StartsWith("Patient:"))
                    {
                        var parts = fullAppointment.Notes.Split('-');
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
                    targetEmail = fullAppointment.Patient?.ApplicationUser?.Email;
                }

                if (string.IsNullOrWhiteSpace(targetPatientName))
                {
                    targetPatientName = fullAppointment.Patient?.ApplicationUser?.FullName ?? "Patient";
                }

                if (!string.IsNullOrEmpty(targetEmail))
                {
                    try
                    {
                        var doctorName = fullAppointment.Doctor?.ApplicationUser?.FullName ?? "Doctor";

                        await _appointmentService.SendAppointmentStatusUpdateAsync(
                            toEmail: targetEmail,
                            patientName: targetPatientName,
                            doctorName: doctorName,
                            date: fullAppointment.AppointmentDate,
                            status: status
                        );

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
