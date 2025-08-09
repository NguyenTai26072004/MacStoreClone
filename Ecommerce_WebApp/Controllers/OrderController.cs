using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_WebApp.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detail() 
        {
            return View();
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
