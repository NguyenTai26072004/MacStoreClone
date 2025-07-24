using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_WebApp.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
