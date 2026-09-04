using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services.IServices;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using clinicManagementSystem.Utilities;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctorRole")]
    public class MedicalRecordsController : Controller
    {
        private readonly IRepository<MedicalRecord> _recordRepo;
        private readonly IRepository<Prescription> _prescriptionRepo;
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
        private readonly IAppointmentService _appointmentService;

        public MedicalRecordsController(
            IRepository<MedicalRecord> recordRepo,
            IRepository<Prescription> prescriptionRepo,
            IRepository<Appointment> appointmentRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IAppointmentService appointmentService)
        {
            _recordRepo = recordRepo;
            _prescriptionRepo = prescriptionRepo;
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _appointmentService = appointmentService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int appointmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account", new { area = SD.IDENTITY_AREA });

            var doctor = await _doctorRepo.GetOneAsync(d => d.ApplicationUserId == userId);
            if (doctor == null) return NotFound("Doctor profile not found.");

            var appointment = await _appointmentRepo.GetOneAsync(
                expression: a => a.AppointmentId == appointmentId,
                includes: [
                    a => a.Patient,
                    a => a.Patient.ApplicationUser,
                    a => a.Doctor,
                    a => a.Doctor.ApplicationUser
                ]
            );

            if (appointment == null) return NotFound();

            if (appointment.DoctorId != doctor.DoctorId) return Forbid();

            string patientDisplayName = GetPatientDisplayName(appointment);

            var viewModel = new CreateMedicalRecordVM
            {
                AppointmentId = appointmentId,
                DoctorId = doctor.DoctorId,
                PatientName = patientDisplayName,
                DoctorName = appointment.Doctor?.ApplicationUser?.FullName ?? $"Doctor #{appointment.DoctorId}",
                Prescriptions = new List<PrescriptionItemVM> { new PrescriptionItemVM() }
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMedicalRecordVM model)
        {
            try
            {
                var record = new MedicalRecord
                {
                    AppointmentId = model.AppointmentId,
                    Diagnosis = string.IsNullOrWhiteSpace(model.Diagnosis) ? "No Specific Diagnosis" : model.Diagnosis,
                    Notes = model.Notes,
                    VisitDate = DateTime.Now
                };

                await _recordRepo.CreateAsync(record);
                await _recordRepo.CommitAsync();

                if (model.Prescriptions != null && model.Prescriptions.Any())
                {
                    foreach (var item in model.Prescriptions)
                    {
                        if (!string.IsNullOrWhiteSpace(item.MedicineName))
                        {
                            var prescription = new Prescription
                            {
                                MedicalRecordId = record.MedicalRecordId,
                                MedicineName = item.MedicineName,
                                Dosage = item.Dosage ?? "",
                                Frequency = item.Frequency ?? "",
                                Duration = item.Duration ?? "",
                                Instructions = item.Instructions ?? ""
                            };
                            await _prescriptionRepo.CreateAsync(prescription);
                        }
                    }
                    await _prescriptionRepo.CommitAsync();
                }

                var appointment = await _appointmentRepo.GetOneAsync(
                    expression: a => a.AppointmentId == model.AppointmentId,
                    includes: [
                        a => a.Patient,
                        a => a.Patient.ApplicationUser,
                        a => a.Doctor,
                        a => a.Doctor.ApplicationUser
                    ]
                );

                if (appointment != null)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    await _appointmentRepo.CommitAsync();

                    string? patientEmail = null;

                    if (!string.IsNullOrWhiteSpace(appointment.Notes))
                    {
                        var match = Regex.Match(appointment.Notes, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                        if (match.Success)
                        {
                            patientEmail = match.Value;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(patientEmail))
                    {
                        patientEmail = appointment.Patient?.ApplicationUser?.Email;
                    }

                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        var doctorName = appointment.Doctor?.ApplicationUser?.FullName ?? "Doctor";
                        var patientName = !string.IsNullOrWhiteSpace(model.PatientName) ? model.PatientName : GetPatientDisplayName(appointment);

                        try
                        {
                            await _appointmentService.SendMedicalRecordReadyEmailAsync(
                                toEmail: patientEmail,
                                patientName: patientName,
                                doctorName: doctorName,
                                diagnosis: record.Diagnosis
                            );

                            TempData["success_notification"] = $"Medical record created and confirmation email sent to {patientEmail}!";
                        }
                        catch (Exception ex)
                        {
                            TempData["error_notification"] = $"Medical record created and status updated to Completed, but email failed: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData["success_notification"] = "Medical record created and status updated to Completed! (No patient email found)";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error_notification"] = $"Failed to create medical record: {ex.Message}";
            }

            return RedirectToAction("Index", "Appointments", new { area = "Doctor" });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int appointmentId)
        {
            var record = await _recordRepo.GetOneAsync(
                expression: r => r.AppointmentId == appointmentId,
includes: [
                    r => r.Prescriptions,
                    r => r.Appointment,
                    r => r.Appointment.Patient,
                    r => r.Appointment.Patient.ApplicationUser,
                    r => r.Appointment.Doctor,
                    r => r.Appointment.Doctor.ApplicationUser
                ]
            );

            if (record == null) return NotFound();

            ViewBag.PatientDisplayName = GetPatientDisplayName(record.Appointment);

            return View(record);
        }

        private string GetPatientDisplayName(Appointment? appointment)
        {
            if (appointment == null) return "Patient";

            if (!string.IsNullOrWhiteSpace(appointment.Notes) && appointment.Notes.StartsWith("Patient:"))
            {
                var parts = appointment.Notes.Split('-');
                var namePart = parts[0].Replace("Patient:", "").Trim();

                if (namePart.Contains('|'))
                {
                    namePart = namePart.Split('|')[0].Trim();
                }

                if (namePart.Contains('('))
                {
                    namePart = namePart.Split('(')[0].Trim();
                }

                if (!string.IsNullOrWhiteSpace(namePart))
                {
                    return namePart;
                }
            }

            return appointment.Patient?.ApplicationUser?.FullName ?? $"Patient #{appointment.PatientId}";
        }
    }
}
