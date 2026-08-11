//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.UI.Services;
//using System.Net;
//using System.Net.Mail;

//namespace clinicManagementSystem.Utilities
//{
//    public class EmailSender : IEmailSender
//    {
//        public Task SendEmailAsync(string email, string subject, string htmlMessage)
//        {
             
//                var client = new SmtpClient("smtp.gmail.com", 587)
//                {
//                    EnableSsl = true,
//                    UseDefaultCredentials = false,
//                    Credentials = new NetworkCredential("yn298024@gmail.com", "adan vamd efrp lvgp")
//                };

//                return client.SendMailAsync(
//                new MailMessage(from: "yn298024@gmail.com",
//                                to: email,
//                                subject,
//                                htmlMessage
//                                )
//                {
//                    IsBodyHtml = true
//                });
//            }
//    }
//}

 
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace clinicManagementSystem.Utilities
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var senderEmail = _configuration["EmailSettings:Email"];
            var senderPassword = _configuration["EmailSettings:Password"];

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    senderEmail,
                    senderPassword
                )
            };

            return client.SendMailAsync(
                new MailMessage(
                    from: senderEmail,
                    to: email,
                    subject: subject,
                    body: htmlMessage
                )
                {
                    IsBodyHtml = true
                });
        }
    }
}
 

