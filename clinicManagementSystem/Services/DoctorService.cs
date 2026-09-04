using Microsoft.AspNetCore.Identity.UI.Services;
using clinicManagementSystem.Services.IServices;

namespace clinicManagementSystem.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IEmailSender _emailSender;

        public DoctorService(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendDoctorAccountCredentialsAsync(string doctorEmail, string doctorName, string setPasswordLink, bool isNewAccount = true)
        {
            string title = isNewAccount ? "Your Doctor Account Has Been Created" : "Your Account Details Updated";
            string introText = isNewAccount
                ? $"Welcome to Clinic Management System, Dr. <strong>{doctorName}</strong>. Your doctor account has been successfully created by administration."
                : $"Hello Dr. <strong>{doctorName}</strong>, your account details have been updated by administration.";

            string body = $@"
                <div style='font-family: Arial, sans-serif; direction: ltr; text-align: left; background-color: #f4f6f9; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                        <h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>{title}</h2>
                        <p style='color: #555555; font-size: 16px;'>{introText}</p>
                        
                        <div style='background-color: #eef2f7; padding: 15px; border-left: 4px solid #3498db; margin: 20px 0;'>
                            <p style='margin: 0 0 10px 0;'><strong>Login Email:</strong> {doctorEmail}</p>
                            <p style='margin: 0 0 15px 0; font-size: 14px;'>Please click the button below to set your account password:</p>
                            <div style='text-align: center; margin: 15px 0;'>
                                <a href='{setPasswordLink}' style='background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Set Your Password</a>
                            </div>
                            <p style='margin: 10px 0 0 0; font-size: 12px; color: #7f8c8d;'>If the button doesn't work, copy and paste this link into your browser:<br/><a href='{setPasswordLink}'>{setPasswordLink}</a></p>
                        </div>

                        <p style='color: #e74c3c; font-size: 14px;'>* This setup link is secure and meant for your use only.</p>
                    </div>
                </div>";

            await _emailSender.SendEmailAsync(doctorEmail, title, body);
        }
    }
}