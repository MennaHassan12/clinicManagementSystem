using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace clinicManagementSystem.Services.IServices
{
    public interface IAccountService
    {
        bool IsLogined(ClaimsPrincipal User);
        Task SendMailAsync(ApplicationUser user, IUrlHelper url, HttpRequest request, EmailType emailType = EmailType.Register);

        Task SendOtpMailAsync(ApplicationUser user);
    }
}

