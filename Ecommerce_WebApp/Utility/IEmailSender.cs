namespace Ecommerce_WebApp.Utility
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailFromTemplateAsync(string toEmail, string subject, string templateName, Dictionary<string, string> replacements);
    }
}