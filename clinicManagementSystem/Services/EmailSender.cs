using System.Net;
using System.Net.Mail;

namespace clinicManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {

            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var portStr = _configuration["EmailSettings:Port"];
            int port = !string.IsNullOrEmpty(portStr) ? int.Parse(portStr) : 587;

            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Clinic Management System";
            var appPassword = _configuration["EmailSettings:AppPassword"];

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(appPassword))
            {
                throw new InvalidOperationException("Email settings (SenderEmail or AppPassword) are missing in appsettings.json.");
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(senderEmail, senderName);
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.Body = htmlMessage;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, appPassword);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await client.SendMailAsync(message);
                }
            }
        }
    }
}