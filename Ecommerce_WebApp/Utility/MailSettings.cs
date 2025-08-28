// File: Utility/MailSettings.cs
namespace Ecommerce_WebApp.Utility
{
    public class MailSettings
    {
        public string FromMail { get; set; } 
        public string DisplayName { get; set; } 
        public string FromPassword { get; set; } 
        public string Host { get; set; } 
        public int Port { get; set; } // Giữ lại hoặc thêm vào secrets.json
    }
}