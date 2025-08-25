using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Helpers;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.Utility;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.Controllers
{

    public class OrderController : Controller
    {
        private readonly AppDbContext _db;

        private readonly IEmailSender _emailSender;
        public OrderController(AppDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        // GET: Hiển thị trang Checkout (code cũ của bạn đã tốt, chỉ thêm [Authorize])
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<ShoppingCart>(CartController.SessionKey);
            if (cart == null || !cart.Items.Any())
            {
                TempData["error"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutVM
            {
                ShoppingCart = cart,
                OrderHeader = new OrderHeader()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                // Dù không điền form, chúng ta vẫn lấy thông tin user để lưu vào đơn hàng
                viewModel.OrderHeader.ApplicationUserId = userId.Value;
                var userFromDb = _db.Users.Find(userId.Value);
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVM viewModel)
        {
            var cart = HttpContext.Session.Get<ShoppingCart>(CartController.SessionKey);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            viewModel.ShoppingCart = cart;

            // Sửa lại cách lấy ApplicationUserId
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                // Nếu người dùng ĐÃ đăng nhập, gán UserId vào đơn hàng
                viewModel.OrderHeader.ApplicationUserId = userId.Value;
            }

            if (ModelState.IsValid)
            {
                // Gán các thông tin còn lại
                viewModel.OrderHeader.OrderDate = DateTime.Now;
                viewModel.OrderHeader.OrderTotal = cart.Items.Sum(i => i.SubTotal);
                viewModel.OrderHeader.OrderStatus = SD.OrderStatusPending;
                viewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;

                // Lưu đơn hàng (OrderHeader)
                _db.OrderHeaders.Add(viewModel.OrderHeader);
                await _db.SaveChangesAsync();

                // Lưu chi tiết đơn hàng (OrderDetail)
                foreach (var item in cart.Items)
                {
                    OrderDetail orderDetail = new()
                    {
                        OrderHeaderId = viewModel.OrderHeader.Id,
                        ProductVariantId = item.VariantId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _db.OrderDetails.Add(orderDetail);
                }
                await _db.SaveChangesAsync();

                // Trừ kho sản phẩm
                foreach (var item in cart.Items)
                {
                    var variantInDb = await _db.ProductVariants.FindAsync(item.VariantId);
                    if (variantInDb != null)
                    {
                        variantInDb.StockQuantity -= item.Quantity;
                    }
                }
                await _db.SaveChangesAsync();

                // Xóa giỏ hàng
                HttpContext.Session.Remove(CartController.SessionKey);

                ////// Lấy email người dùng đã điền trong form
                ////var customerEmail = viewModel.OrderHeader.Email; // Bạn cần thêm trường Email vào OrderHeader.cs

                ////// Tạo nội dung email (thường là render một Razor View thành chuỗi HTML)
                ////string emailSubject = $"Xác nhận đơn hàng #{viewModel.OrderHeader.Id}";
                ////string emailBody = $"Cảm ơn bạn đã đặt hàng..."; // Sẽ làm đẹp sau

                //await _emailSender.SendEmailAsync(customerEmail, emailSubject, emailBody);

                return RedirectToAction("ThankYou", new { orderId = viewModel.OrderHeader.Id });
            }

            // Nếu model state không hợp lệ
            return View(viewModel);
        }


        [Authorize] 
        public async Task<IActionResult> Index()
        {
            // Lấy ID của người dùng đang đăng nhập
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Truy vấn tất cả các đơn hàng của người dùng đó, sắp xếp mới nhất lên đầu
            List<OrderHeader> userOrders = await _db.OrderHeaders
                .Where(oh => oh.ApplicationUserId == userId)
                .OrderByDescending(oh => oh.OrderDate)
                .ToListAsync();

            return View(userOrders);
        }


        // GET: /Order/Detail/{orderId}
        [Authorize]
        public async Task<IActionResult> Detail(int orderId)
        {
            // Lấy thông tin đơn hàng, bao gồm cả chi tiết các sản phẩm bên trong
            var orderHeader = await _db.OrderHeaders
                .Include(oh => oh.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(oh => oh.Id == orderId);

            if (orderHeader == null) return NotFound();

            // Logic bảo mật: Đảm bảo người dùng chỉ có thể xem đơn hàng của chính họ
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (orderHeader.ApplicationUserId != userId)
            {
                // Có thể chuyển đến trang AccessDenied hoặc NotFound
                return NotFound();
            }

            return View(orderHeader);
        }

        // Action cho trang đặt hàng thành công
        public IActionResult ThankYou(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}