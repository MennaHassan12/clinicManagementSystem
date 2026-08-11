using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services; 
using System.Text.RegularExpressions; 

namespace clinicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AppointmentsController : Controller
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<clinicManagementSystem.Models.Doctor> _doctorRepo;
       // private readonly IRepository<clinicManagementSystem.Models.Patient> _patientRepo;
        private readonly IRepository<Department> _departmentRepo;
       // private readonly IRepository<DoctorSchedule> _scheduleRepo;
        private readonly IRepository<MedicalRecord> _recordRepo;
        private readonly IEmailSender _emailSender; 

        public AppointmentsController(
            IRepository<Appointment> appointmentRepo,
            IRepository<clinicManagementSystem.Models.Doctor> doctorRepo,
            //IRepository<clinicManagementSystem.Models.Patient> patientRepo,
            IRepository<Department> departmentRepo,
            //IRepository<DoctorSchedule> scheduleRepo,
            IRepository<MedicalRecord> recordRepo,
            IEmailSender emailSender)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo = doctorRepo;
            //_patientRepo = patientRepo;
            _departmentRepo = departmentRepo;
            //_scheduleRepo = scheduleRepo;
            _recordRepo = recordRepo;
            _emailSender = emailSender;
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

            //Searching

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
                            <p>Your appointment status with Dr. <b>{doctorName}</b> has been updated by Admin to: <b style='color: #0d6efd;'>{status}</b>.</p>
                            <p><b>Appointment Date:</b> {appointment.AppointmentDate:dd MMMM, yyyy}</p>
                            <br/>
                            <p>Thank you for choosing Clinic Management System.</p>";

                        try
                        {
                            await _emailSender.SendEmailAsync(patientEmail, $"Appointment Status Update - {status}", emailBody);
                            TempData["success_notification"] = $"Appointment status updated to {status} and email sent to {patientEmail}!";
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

                    TempData["success_notification"] = "Appointment deleted successfully by Admin!";
                }
                else
                {
                    TempData["error_notification"] = "Appointment not found!";
                }
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["error_notification"] = "Cannot delete: " + innerMessage;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
