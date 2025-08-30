using Ecommerce_WebApp.Models;

namespace Ecommerce_WebApp.Services
{
    public interface IMomoService
    {
        Task<string> CreatePaymentUrlAsync(OrderHeader order, HttpContext context);
    }
}