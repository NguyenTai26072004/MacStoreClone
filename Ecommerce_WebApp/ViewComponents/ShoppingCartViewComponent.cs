using Ecommerce_WebApp.Controllers;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Ecommerce_WebApp.Helpers;


namespace Ecommerce_WebApp.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cart = HttpContext.Session.Get<ShoppingCart>(CartController.SessionKey) ?? new ShoppingCart();
            return View("Default", cart);
        }
    }
}
