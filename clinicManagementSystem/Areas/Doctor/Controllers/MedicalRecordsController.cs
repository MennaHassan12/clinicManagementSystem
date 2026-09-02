using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using clinicManagementSystem.Utilities;

namespace clinicManagementSystem.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Policy = "RequireDoctor")]
    public class MedicalRecordsController : Controller
    {
        private readonly IRepository<MedicalRecord> _recordRepo;
        private readonly IRepository<Prescription> _prescriptionRepo;
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
        private readonly IEmailSender _emailSender;

        public MedicalRecordsController(
            IRepository<MedicalRecord> recordRepo,
            IRepository<Prescription> prescriptionRepo,
            IRepository<Appointment> appointmentRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            IEmailSender emailSender)
        {
            _recordRepo = recordRepo;
            _prescriptionRepo = prescriptionRepo;
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            _emailSender = emailSender;
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

            string patientDisplayName = "";

            if (!string.IsNullOrWhiteSpace(appointment.Notes) && appointment.Notes.StartsWith("Patient:"))
            {
                var parts = appointment.Notes.Split('-');
                var namePart = parts[0].Replace("Patient:", "").Trim();
                if (namePart.Contains('('))
                {
                    namePart = namePart.Split('(')[0].Trim();
                }
                if (!string.IsNullOrWhiteSpace(namePart))
                {
                    patientDisplayName = namePart;
                }
            }

            if (string.IsNullOrWhiteSpace(patientDisplayName))
            {
                patientDisplayName = appointment.Patient?.ApplicationUser?.FullName;
            }

            if (string.IsNullOrWhiteSpace(patientDisplayName))
            {
                patientDisplayName = $"Patient #{appointment.PatientId}";
            }

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
                        var doctorName = appointment.Doctor?.ApplicationUser?.FullName ?? "your doctor";
                        var patientName = !string.IsNullOrWhiteSpace(model.PatientName) ? model.PatientName : appointment.Patient?.ApplicationUser?.FullName ?? "Patient";

                        string emailBody = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                                <h2 style='color: #0d6efd;'>Visit Completed & Prescription Ready</h2>
                                <p>Dear <b>{patientName}</b>,</p>
                                <p>Your appointment with <b>{doctorName}</b> has been marked as <b>Completed</b>.</p>
                                <p>Your medical record and prescription are now available on your patient portal.</p>
<p><b>Diagnosis:</b> {record.Diagnosis}</p>
                                <br/>
                                <p style='color: #6c757d; font-size: 12px;'>Thank you for choosing Clinic Management System.</p>
                            </div>";

                        try
                        {
                            await _emailSender.SendEmailAsync(patientEmail, "Appointment Completed - Medical Record Ready", emailBody);
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
                includes: [r => r.Prescriptions]
            );

            if (record == null) return NotFound();

            return View(record);
        }
    }
}
