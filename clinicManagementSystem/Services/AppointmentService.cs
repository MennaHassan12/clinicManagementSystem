using clinicManagementSystem.Models;
using clinicManagementSystem.Services.IServices;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace clinicManagementSystem.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IEmailSender _emailSender;

        public AppointmentService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendAppointmentConfirmationAsync(string toEmail, string patientName, string doctorName, DateOnly date, TimeOnly time, string? setPasswordLink = null)
        {
            string accountInfoHtml = !string.IsNullOrEmpty(setPasswordLink) ? $@"
                <div style='background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin-top: 20px; border-radius: 4px;'>
                    <h4 style='margin: 0 0 8px 0; color: #0d6efd;'>Account Created</h4>
                    <p style='margin: 0 0 12px 0; font-size: 14px;'>An account has been created for you to manage your appointments and medical records. Please click below to set your account password:</p>
                    <div style='text-align: center; margin: 15px 0;'>
                        <a href='{setPasswordLink}' style='background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Set Your Password</a>
                    </div>
                    <p style='margin: 0; font-size: 12px; color: #6c757d;'>If the button does not work, copy and paste this link into your browser:<br/><a href='{setPasswordLink}'>{setPasswordLink}</a></p>
                </div>" : "";

            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #0d6efd; color: white; padding: 20px; text-align: center;'>
                    <h2 style='margin: 0;'>Clinic Management System</h2>
                    <p style='margin: 5px 0 0 0;'>Appointment Confirmation</p>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Dear <strong>{patientName}</strong>,</p>
                    <p>Your appointment has been successfully booked!</p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Doctor:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>Dr. {doctorName}</td></tr>
                        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Date:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{date:dd MMMM, yyyy}</td></tr>
                        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Time:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{DateTime.Today.Add(time.ToTimeSpan()):hh:mm tt}</td></tr>
                    </table>

                    {accountInfoHtml}

                    <p style='margin-top: 25px; font-size: 0.9em; color: #6c757d;'>Thank you for choosing our clinic.</p>
                </div>
            </div>";

            await _emailSender.SendEmailAsync(toEmail, "Appointment Confirmation & Account Setup", body);
        }

        public async Task SendAppointmentStatusUpdateAsync(string toEmail, string patientName, string doctorName, DateOnly date, AppointmentStatus status)
        {
            string statusColor = status switch
            {
                AppointmentStatus.Confirmed => "#198754",
                AppointmentStatus.Cancelled => "#dc3545",
                _ => "#ffc107"
            };
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: {statusColor}; color: white; padding: 20px; text-align: center;'>
                    <h2 style='margin: 0;'>Clinic Management System</h2>
                    <p style='margin: 5px 0 0 0;'>Appointment Status Update</p>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Dear <strong>{patientName}</strong>,</p>
                    <p>Your appointment status with <strong>Dr. {doctorName}</strong> on <strong>{date:dd MMMM, yyyy}</strong> has been updated to:</p>
                    <h3 style='color: {statusColor}; text-align: center; background: #f8f9fa; padding: 12px; border-radius: 5px; border: 1px solid #ddd;'>{status}</h3>
                    <p style='margin-top: 25px; font-size: 0.9em; color: #6c757d;'>Thank you for choosing our clinic.</p>
                </div>
            </div>";

            await _emailSender.SendEmailAsync(toEmail, $"Appointment Status Update - {status}", body);
        }

        public async Task SendMedicalRecordReadyEmailAsync(string toEmail, string patientName, string doctorName, string diagnosis)
        {
            string subject = "Appointment Completed - Medical Record Ready";
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #0d6efd; color: white; padding: 20px; text-align: center;'>
                    <h2 style='margin: 0;'>Clinic Management System</h2>
                    <p style='margin: 5px 0 0 0;'>Visit Completed & Prescription Ready</p>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Dear <strong>{patientName}</strong>,</p>
                    <p>Your appointment with <strong>Dr. {doctorName}</strong> has been marked as <b style='color: #198754;'>Completed</b>.</p>
                    <p>Your medical record and prescription are now available on your patient portal.</p>
                    
                    <div style='background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 12px; margin: 15px 0; border-radius: 4px;'>
                        <p style='margin: 0;'><strong>Diagnosis:</strong> {diagnosis}</p>
                    </div>

                    <p style='margin-top: 25px; font-size: 0.9em; color: #6c757d;'>Thank you for choosing Clinic Management System.</p>
                </div>
            </div>";

            await _emailSender.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAppointmentBookingEmailAsync(string toEmail, string patientName, string doctorName, DateOnly date, TimeOnly time)
        {
            string subject = "Appointment Confirmation - Clinic Management System";
            string formattedTime = DateTime.Today.Add(time.ToTimeSpan()).ToString("hh:mm tt");
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #0d6efd; color: white; padding: 20px; text-align: center;'>
                    <h2 style='margin: 0;'>Clinic Management System</h2>
                    <p style='margin: 5px 0 0 0;'>Appointment Booking Confirmation</p>
                </div>
                <div style='padding: 20px; color: #333;'>
                    <p>Dear <strong>{patientName}</strong>,</p>
                    <p>Your appointment has been successfully booked with status <b style='color: #ffc107;'>Pending Confirmation</b>.</p>
<div style='background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                        <p style='margin: 0 0 8px 0;'><strong>Doctor:</strong> {doctorName}</p>
                        <p style='margin: 0 0 8px 0;'><strong>Date:</strong> {date:dd MMMM, yyyy} ({date.DayOfWeek})</p>
                        <p style='margin: 0;'><strong>Time:</strong> {formattedTime}</p>
                    </div>

                    <p style='margin-top: 25px; font-size: 0.9em; color: #6c757d;'>Thank you for choosing Clinic Management System.</p>
                </div>
            </div>";

            await _emailSender.SendEmailAsync(toEmail, subject, body);
        }
    }
}