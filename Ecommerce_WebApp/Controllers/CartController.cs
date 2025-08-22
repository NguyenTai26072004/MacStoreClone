using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.Helpers; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Ecommerce_WebApp.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _db;
        public const string SessionKey = "ShoppingCart";

        public CartController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET: /Cart/Index
        /// Hiển thị trang giỏ hàng chi tiết.
        /// </summary>
        public IActionResult Index()
        {
            // Đọc giỏ hàng từ Session. Nếu chưa có, tạo mới.
            var cart = HttpContext.Session.Get<ShoppingCart>(SessionKey) ?? new ShoppingCart();
            return View(cart);
        }

        public IActionResult RenderCartDropdown()
        {
            return ViewComponent("ShoppingCart");
        }

        /// <summary>
        /// POST: /Cart/AddToCart
        /// API để thêm một phiên bản sản phẩm vào giỏ hàng (dùng cho AJAX).
        /// </summary>
        [HttpPost]
        public IActionResult AddToCart(int variantId, int quantity)
        {
            // Lấy thông tin chi tiết của phiên bản sản phẩm
            var variant = _db.ProductVariants
                .Include(v => v.Product).ThenInclude(p => p.Images)
                .Include(v => v.VariantValues).ThenInclude(vv => vv.AttributeValue)
                .FirstOrDefault(v => v.Id == variantId);

            if (variant == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }
            if (variant.StockQuantity < quantity)
            {
                return Json(new { success = false, message = $"Chỉ còn {variant.StockQuantity} sản phẩm trong kho." });
            }

            // Lấy giỏ hàng từ Session, hoặc tạo mới nếu chưa có
            var cart = HttpContext.Session.Get<ShoppingCart>(SessionKey) ?? new ShoppingCart();

            // Tìm xem sản phẩm đã có trong giỏ chưa
            var cartItem = cart.Items.FirstOrDefault(i => i.VariantId == variantId);

            if (cartItem == null) // Nếu chưa có, tạo mới
            {
                var variantDesc = string.Join(" / ", variant.VariantValues.Select(vv => vv.AttributeValue.Value));
                cart.Items.Add(new CartItem
                {
                    VariantId = variantId,
                    ProductName = variant.Product.Name,
                    VariantDescription = variantDesc,
                    Quantity = quantity,
                    Price = variant.Price,
                    ImageUrl = variant.Product.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? "/images/placeholder.png"
                });
            }
            else // Nếu đã có, chỉ tăng số lượng
            {
                // Kiểm tra lại tồn kho khi thêm
                if (variant.StockQuantity < cartItem.Quantity + quantity)
                {
                    return Json(new { success = false, message = $"Số lượng trong kho không đủ. Bạn đã có {cartItem.Quantity} trong giỏ." });
                }
                cartItem.Quantity += quantity;
            }

            // Lưu giỏ hàng trở lại Session
            HttpContext.Session.Set(SessionKey, cart);

            // Trả về kết quả thành công và tổng số lượng mới
            return Json(new { success = true, cartItemCount = cart.Items.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int variantId, int quantity)
        {
            var cart = HttpContext.Session.Get<ShoppingCart>(SessionKey) ?? new ShoppingCart();
            var cartItem = cart.Items.FirstOrDefault(i => i.VariantId == variantId);

            // Kiểm tra tồn kho trước khi cập nhật
            var variantInDb = _db.ProductVariants.Find(variantId);
            if (quantity <= 0)
            {
                // Nếu người dùng nhập số lượng <= 0, coi như xóa sản phẩm
                cart.Items.RemoveAll(i => i.VariantId == variantId);
            }
            else if (variantInDb != null && quantity > variantInDb.StockQuantity)
            {
                return Json(new { success = false, message = $"Chỉ còn {variantInDb.StockQuantity} sản phẩm trong kho." });
            }
            else if (cartItem != null)
            {
                cartItem.Quantity = quantity;
            }

            HttpContext.Session.Set(SessionKey, cart);

            // Trả về dữ liệu mới để JavaScript cập nhật giao diện
            return Json(new
            {
                success = true,
                cartItemCount = cart.Items.Sum(i => i.Quantity),
                newSubTotal = cartItem?.SubTotal.ToString("N0"),
                newTotal = cart.Items.Sum(i => i.SubTotal).ToString("N0")
            });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int variantId)
        {
            var cart = HttpContext.Session.Get<ShoppingCart>(SessionKey) ?? new ShoppingCart();
            cart.Items.RemoveAll(i => i.VariantId == variantId);
            HttpContext.Session.Set(SessionKey, cart);

            return Json(new
            {
                success = true,
                cartItemCount = cart.Items.Sum(i => i.Quantity),
                newTotal = cart.Items.Sum(i => i.SubTotal).ToString("N0")
            });
        }
    }
}