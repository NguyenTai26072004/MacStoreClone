using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Ecommerce_WebApp.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment; // Thêm dịch vụ này

        public EmailSender(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment; // Gán vào
        }

        // Ghi đè phương thức gốc, nhưng chúng ta sẽ không dùng nó trực tiếp nữa
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Có thể tạo một Dictionary ở đây và gọi phương thức mới nếu muốn
            var replacements = new Dictionary<string, string>
            {
                { "{{Content}}", htmlMessage } // Ví dụ
            };
            return SendEmailFromTemplateAsync(email, subject, "DefaultTemplate.html", replacements);
        }

        // TẠO PHƯƠNG THỨC MỚI, MẠNH MẼ HƠN
        public async Task SendEmailFromTemplateAsync(string email, string subject, string templateName, Dictionary<string, string> replacements)
        {
            // 1. Lấy đường dẫn đến tệp mẫu trong wwwroot
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", templateName);

            // 2. Đọc toàn bộ nội dung của tệp mẫu
            var templateContent = await File.ReadAllTextAsync(templatePath);

            // 3. Thay thế các placeholder bằng giá trị thực
            foreach (var (placeholder, value) in replacements)
            {
                templateContent = templateContent.Replace(placeholder, value);
            }

            // 4. Lấy thông tin người gửi từ secrets.json
            var fromMail = _configuration["EmailSettings:FromMail"];
            var fromPassword = _configuration["EmailSettings:FromPassword"];

            if (string.IsNullOrEmpty(fromMail) || string.IsNullOrEmpty(fromPassword))
            {
                // Ghi log lỗi, không gửi email
                return;
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = subject,
                Body = templateContent, 
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(email));

            // Cấu hình SMTP và gửi đi
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(message);
        }
    }
}