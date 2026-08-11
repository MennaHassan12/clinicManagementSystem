using clinicManagementSystem.Areas.Identity.Controllers;
using clinicManagementSystem.Data;
using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services.IServices;
using clinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace clinicManagementSystem.Services
{
    public enum EmailType
    {
        Register,
        ResendConfirmation,
        ForgetPassword
    }

    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
         
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;

        public AccountService(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ApplicationDbContext context, IRepository<ApplicationUserOTP> applicationUserOTPRepository)
        {
            _userManager = userManager;
            _emailSender = emailSender;
             
            _applicationUserOTPRepository = applicationUserOTPRepository;
        }

        public bool IsLogined(ClaimsPrincipal User)
        {
            if (User is not null && User.Identity.IsAuthenticated)
                return true;

            return false;
        }

        public async Task SendMailAsync(ApplicationUser user, IUrlHelper url, HttpRequest request, EmailType emailType = EmailType.Register)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var link = url.Action(
                action: nameof(AccountController.Confirm),
                controller: SD.ACCOUNT_CONTROLER,
                values: new { area = SD.IDENTITY_AREA, token, id = user.Id },
                protocol: request.Scheme,
                host: request.Host.Value);

            string subject = string.Empty;
            string message = string.Empty;

            switch (emailType)
            {
                case EmailType.Register:
                    {
                        subject = "Confirm Your Account – Clinic System";
                        message = BuildConfirmationEmail(
                            title: "Confirm Your Email Address",
                            introText: "Thanks for creating an account with Clinic System. Please confirm your email address to activate your account and start using the diagnostic platform.",
                            link: link,
                            buttonText: "Confirm My Account");
                    }
                    break;

                case EmailType.ResendConfirmation:
                    {
                        subject = "Resend: Confirm Your Account – Clinic System";
                        message = BuildConfirmationEmail(
                            title: "Confirm Your Email Address",
                            introText: "We received a request to resend your account confirmation email. Please click the button below to confirm your email address and activate your account.",
                            link: link,
                            buttonText: "Confirm My Account");
                    }
                    break;

                case EmailType.ForgetPassword:
                    {
                        subject = "Reset Your Password – Clinic System";
                        message = BuildConfirmationEmail(
                            title: "Reset Your Password",
                            introText: "We received a request to reset your Clinic System password. Click the button below to choose a new password. If you didn't make this request, you can ignore this email.",
                            link: link,
                            buttonText: "Reset My Password");
                    }
                    break;
            }

            await _emailSender.SendEmailAsync(user.Email, subject, message);
        }

        // === الميثود الجديدة بتاعة الـ OTP (لصفحة Forget Password) ===
        public async Task SendOtpMailAsync(ApplicationUser user)
        {
            var otpCode = Random.Shared.Next(100000, 999999).ToString();

            var otpEntity = new ApplicationUserOTP
            {
                ApplicationUserId = user.Id,
                OTP = otpCode,
                CreateAt = DateTime.Now,
                ValidTo = DateTime.Now.AddMinutes(10),
                IsUsed = false
            };

            await _applicationUserOTPRepository.CreateAsync(otpEntity);   // ← الريبو
            await _applicationUserOTPRepository.CommitAsync();

            string subject = "Your Password Reset Code – Clinic System";
            string message = BuildOtpEmail(
                title: "Reset Your Password",
                introText: "We received a request to reset your Clinic System password. Enter the code below to continue. This code expires in 10 minutes.",
                otp: otpCode);

            await _emailSender.SendEmailAsync(user.Email, subject, message);
        }

        // Reusable, inline-styled HTML email template.
        // Email clients strip <style> blocks and JS, so every style below is inline
        // and layout uses <table> for maximum compatibility (Outlook, Gmail, etc).
        private string BuildConfirmationEmail(string title, string introText, string link, string buttonText)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <body style='margin:0;padding:0;background-color:#f4f6fb;font-family:Segoe UI, Tahoma, Geneva, Verdana, sans-serif;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6fb;padding:40px 0;'>
                    <tr>
                        <td align='center'>
                            <table role='presentation' width='460' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:12px;border:1px solid #e7e9f2;'>

                                <!-- Header -->
                                <tr>
                                    <td style='padding:28px 32px 16px;'>
                                        <p style='color:#1c2340;font-size:17px;font-weight:700;margin:0;'>Clinic System</p>
                                        <p style='color:#8892b3;font-size:11px;letter-spacing:1.5px;margin:2px 0 0;text-transform:uppercase;'>Diagnostic Platform</p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='padding:0 32px;'>
                                        <div style='border-top:1px solid #edeff6;'></div>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style='padding:24px 32px 8px;'>
                                        <h2 style='color:#16265b;font-size:17px;margin:0 0 10px;font-weight:700;'>{title}</h2>
                                        <p style='color:#4b5670;font-size:14px;line-height:1.7;margin:0 0 22px;'>{introText}</p>

                                        <a href='{link}' style='display:inline-block;padding:11px 26px;background-color:#2f5fe0;color:#ffffff;font-size:14px;font-weight:700;text-decoration:none;border-radius:8px;'>{buttonText}</a>

                                        <p style='color:#8892b3;font-size:12px;line-height:1.6;margin:24px 0 0;'>
                                            If the button doesn't work, use this link:<br />
                                            <a href='{link}' style='color:#2f5fe0;word-break:break-all;'>{link}</a>
                                        </p>
                                        <p style='color:#8892b3;font-size:12px;line-height:1.6;margin:14px 0 0;'>
                                            Didn't request this? You can ignore this email.
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style='padding:20px 32px 26px;'>
                                        <p style='color:#a7afc9;font-size:11px;margin:0;line-height:1.6;'>
                                            &copy; 2026 Clinic System. All Rights Reserved.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
        }

        // Same visual identity as the confirmation email, but with a large,
        // easy-to-read OTP code block instead of a button.
        private string BuildOtpEmail(string title, string introText, string otp)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <body style='margin:0;padding:0;background-color:#f4f6fb;font-family:Segoe UI, Tahoma, Geneva, Verdana, sans-serif;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6fb;padding:40px 0;'>
                    <tr>
                        <td align='center'>
                            <table role='presentation' width='460' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:12px;border:1px solid #e7e9f2;'>

                                <!-- Header -->
                                <tr>
                                    <td style='padding:28px 32px 16px;'>
                                        <p style='color:#1c2340;font-size:17px;font-weight:700;margin:0;'>Clinic System</p>
                                        <p style='color:#8892b3;font-size:11px;letter-spacing:1.5px;margin:2px 0 0;text-transform:uppercase;'>Diagnostic Platform</p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='padding:0 32px;'>
                                        <div style='border-top:1px solid #edeff6;'></div>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style='padding:24px 32px 8px;'>
                                        <h2 style='color:#16265b;font-size:17px;margin:0 0 10px;font-weight:700;'>{title}</h2>
                                        <p style='color:#4b5670;font-size:14px;line-height:1.7;margin:0 0 24px;'>{introText}</p>

                                        <!-- OTP Code Box -->
                                        <table role='presentation' width='100%' cellpadding='0' cellspacing='0'>
                                            <tr>
                                                <td align='center' style='padding:0 0 24px;'>
                                                    <div style='display:inline-block;background-color:#f0f3fb;border:1.5px dashed #2f5fe0;border-radius:12px;padding:16px 36px;'>
                                                        <span style='font-size:32px;font-weight:800;letter-spacing:10px;color:#16265b;font-family:Consolas, Menlo, monospace;'>{otp}</span>
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style='color:#8892b3;font-size:12px;line-height:1.6;margin:0 0 6px;text-align:center;'>
                                            This code expires in <b style='color:#4b5670;'>10 minutes</b>.
                                        </p>
                                        <p style='color:#8892b3;font-size:12px;line-height:1.6;margin:18px 0 0;'>
                                            Didn't request this? You can safely ignore this email — your password won't be changed.
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style='padding:20px 32px 26px;'>
                                        <p style='color:#a7afc9;font-size:11px;margin:0;line-height:1.6;'>
                                            &copy; 2026 Clinic System. All Rights Reserved.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
        }
    }
}