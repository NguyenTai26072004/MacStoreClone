using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_WebApp.Controllers
{
    public class CheckoutController : Controller
    {

        [HttpGet]
        public IActionResult Index()
        {
            // Logic để chuẩn bị dữ liệu cho trang thanh toán...
            // var checkoutViewModel = ...;

            // Dòng này sẽ tìm và render file Views/Checkout/Index.cshtml
            return View(/* checkoutViewModel */ );
        }

        //// [HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult ProcessCheckout(/* CheckoutViewModel model */)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        // Nếu dữ liệu không hợp lệ, quay trở lại view thanh toán để hiển thị lỗi
        //        return View("Index", /* model */);
        //    }

        //    // Logic xử lý đơn hàng...
        //    // var newOrderId = ...;

        //    // Sau khi xử lý thành công, chuyển hướng người dùng đến Action ThankYou trong OrderController
        //    return RedirectToAction("ThankYou", "Order", new { orderId = newOrderId });
        //}
    }
}
