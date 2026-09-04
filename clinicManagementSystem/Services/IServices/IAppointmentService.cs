using clinicManagementSystem.Models;

namespace clinicManagementSystem.Services.IServices
{
    public interface IAppointmentService
    {
        Task SendAppointmentBookingEmailAsync(string toEmail, string patientName, string doctorName, DateOnly date, TimeOnly time);
        Task SendMedicalRecordReadyEmailAsync(string toEmail, string patientName, string doctorName, string diagnosis);
        Task SendAppointmentConfirmationAsync(string toEmail, string patientName, string doctorName, DateOnly date, TimeOnly time, string? setPasswordLink = null);
        Task SendAppointmentStatusUpdateAsync(string toEmail, string patientName, string doctorName, DateOnly date, AppointmentStatus status);
    }
}