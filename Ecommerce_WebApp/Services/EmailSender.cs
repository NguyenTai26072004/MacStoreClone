using Ecommerce_WebApp.Utility; 
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options; 
using System.Net;
using System.Net.Mail;
using System.IO; 
using System.Collections.Generic; 
using System.Threading.Tasks; 

namespace Ecommerce_WebApp.Services
{
    // Lớp này kế thừa cả IEmailSender của Identity và IEmailSender của chúng ta
    public class EmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Utility.IEmailSender
    {
        private readonly MailSettings _mailSettings;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Tiêm IOptions<MailSettings> thay vì IConfiguration
        public EmailSender(IOptions<MailSettings> mailSettings, IWebHostEnvironment webHostEnvironment)
        {
            _mailSettings = mailSettings.Value;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- TRIỂN KHAI PHƯƠNG THỨC GỬI EMAIL CHUNG ---
        // Phương thức này sẽ là "trái tim" gửi tất cả các loại email
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Lấy thông tin người gửi từ MailSettings đã được tiêm vào
            var fromMail = _mailSettings.FromMail;
            var fromPassword = _mailSettings.FromPassword;
            var displayName = _mailSettings.DisplayName;
            var host = _mailSettings.Host;
            var port = _mailSettings.Port;

            if (string.IsNullOrEmpty(fromMail) || string.IsNullOrEmpty(fromPassword))
            {
                // Xử lý lỗi nếu cấu hình bị thiếu
                throw new InvalidOperationException("Email settings are not configured properly.");
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromMail, displayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true // Luôn cho phép HTML
            };
            message.To.Add(new MailAddress(toEmail));

            // Cấu hình SmtpClient và gửi đi
            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(message);
        }

        // --- PHƯƠNG THỨC GỬI EMAIL DỰA TRÊN TEMPLATE (TÙY CHỌN, NÂNG CAO) ---
        // Giữ lại phương thức cũ của bạn, nhưng dùng phương thức SendEmailAsync mới
        public async Task SendEmailFromTemplateAsync(string toEmail, string subject, string templateName, Dictionary<string, string> replacements)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", templateName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Email template not found at {templatePath}");
            }

            var templateContent = await File.ReadAllTextAsync(templatePath);

            foreach (var (placeholder, value) in replacements)
            {
                templateContent = templateContent.Replace(placeholder, value);
            }

            // Gọi lại hàm gửi email chung
            await SendEmailAsync(toEmail, subject, templateContent);
        }
    }
}