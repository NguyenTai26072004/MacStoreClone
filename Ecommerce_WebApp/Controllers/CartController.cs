using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_WebApp.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // [HttpPost]
        // Xử lý việc thêm sản phẩm vào giỏ hàng (thường được gọi qua AJAX)
        public IActionResult AddToCart(int productId, int quantity)
        {
            // Ghi chú:
            // 1. Dùng productId để lấy thông tin sản phẩm.
            // 2. Gọi service để thêm sản phẩm vào giỏ hàng.
            // 3. Trả về một JSON result để thông báo thành công và cập nhật UI.
            return Json(new { success = true, message = "Sản phẩm đã được thêm vào giỏ!" });
        }

        // [HttpPost]
        // Cập nhật số lượng của một sản phẩm trong giỏ hàng (thường dùng AJAX)
        public IActionResult UpdateCart(int productId, int quantity)
        {
            // Ghi chú:
            // 1. Gọi service để cập nhật số lượng.
            // 2. Tính toán lại thành tiền của sản phẩm và tổng tiền của giỏ hàng.
            // 3. Trả về JSON chứa các thông tin đã cập nhật.
            return Json(new { success = true, newTotal = "50.000.000₫" });
        }

        // [HttpPost]
        // Xóa một sản phẩm khỏi giỏ hàng (thường dùng AJAX)
        public IActionResult RemoveFromCart(int productId)
        {
            // Ghi chú:
            // 1. Gọi service để xóa sản phẩm.
            // 2. Trả về JSON thông báo thành công.
            return Json(new { success = true });
        }
    }
}
