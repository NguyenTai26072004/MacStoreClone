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
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.Controllers
{

    public class OrderController : Controller
    {
        private readonly AppDbContext _db;

        private readonly Utility.IEmailSender _emailSender;
        public OrderController(AppDbContext db, Utility.IEmailSender emailSender)
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
                TempData["error"] = "Giỏ hàng đã hết hạn hoặc trống.";
                return RedirectToAction("Index", "Cart");
            }
            viewModel.ShoppingCart = cart;

            // Liên kết với người dùng nếu đã đăng nhập
            LinkUserToOrderHeader(viewModel.OrderHeader);

            ModelState.Remove("OrderHeader.ApplicationUser");
            if (ModelState.IsValid)
            {
                // Thực hiện toàn bộ quy trình đặt hàng
                await ProcessOrder(viewModel);

                // Gửi email xác nhận
                await SendConfirmationEmail(viewModel);

                // Dọn dẹp và chuyển hướng
                HttpContext.Session.Remove(CartController.SessionKey);
                return RedirectToAction("ThankYou", new { orderId = viewModel.OrderHeader.Id });
            }

            // Nếu có lỗi, quay lại form
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
        .Include(oh => oh.OrderDetails)
            .ThenInclude(od => od.ProductVariant)
                .ThenInclude(pv => pv.VariantValues)
                    .ThenInclude(vv => vv.AttributeValue)
                        .ThenInclude(av => av.Attribute)
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

        /// <summary>
        /// Gán ApplicationUserId vào đơn hàng nếu người dùng đã đăng nhập.
        /// </summary>
        private void LinkUserToOrderHeader(OrderHeader orderHeader)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    orderHeader.ApplicationUserId = userId.Value;
                }
            }
        }

        /// <summary>
        /// Thực hiện việc lưu đơn hàng, chi tiết đơn hàng và trừ kho.
        /// </summary>
        private async Task ProcessOrder(CheckoutVM viewModel)
        {
            // 1. Hoàn thiện thông tin và lưu OrderHeader
            var orderHeader = viewModel.OrderHeader;
            orderHeader.OrderDate = DateTime.Now;
            orderHeader.OrderTotal = viewModel.ShoppingCart.Items.Sum(i => i.SubTotal);
            orderHeader.OrderStatus = SD.OrderStatusPending;
            orderHeader.PaymentStatus = SD.PaymentStatusPending;
            _db.OrderHeaders.Add(orderHeader);
            await _db.SaveChangesAsync();

            // 2. Tạo và lưu OrderDetails
            var orderDetails = viewModel.ShoppingCart.Items.Select(item => new OrderDetail
            {
                OrderHeaderId = orderHeader.Id,
                ProductVariantId = item.VariantId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();
            _db.OrderDetails.AddRange(orderDetails);

            // 3. Trừ kho sản phẩm
            var variantIds = viewModel.ShoppingCart.Items.Select(i => i.VariantId).ToList();
            var variantsInDb = await _db.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToListAsync();
            foreach (var item in viewModel.ShoppingCart.Items)
            {
                var variant = variantsInDb.FirstOrDefault(v => v.Id == item.VariantId);
                if (variant != null)
                {
                    variant.StockQuantity -= item.Quantity;
                }
            }

            // 4. Lưu tất cả thay đổi (OrderDetail và cập nhật Stock)
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Chuẩn bị và gửi email xác nhận đơn hàng.
        /// </summary>
        private async Task SendConfirmationEmail(CheckoutVM viewModel)
        {
            try
            {
                string subject = $"MacStore - Xác nhận đơn hàng #{viewModel.OrderHeader.Id}";
                string orderItemsHtml = await BuildOrderItemsHtml(viewModel.ShoppingCart.Items);

                var replacements = new Dictionary<string, string>
                {
                    { "{{CustomerName}}", viewModel.OrderHeader.FullName },
                    { "{{OrderId}}", viewModel.OrderHeader.Id.ToString() },
                    { "{{OrderItems}}", orderItemsHtml },
                    { "{{OrderTotal}}", viewModel.OrderHeader.OrderTotal.ToString("N0") },
                    { "{{ShippingAddress}}", $"{viewModel.OrderHeader.Address}, {viewModel.OrderHeader.Ward}, {viewModel.OrderHeader.District}, {viewModel.OrderHeader.City}" }
                };

                await _emailSender.SendEmailFromTemplateAsync(viewModel.OrderHeader.Email, subject, "OrderConfirmation.html", replacements);
            }
            catch (Exception)
            {
                // Ghi log lỗi nhưng không làm crash chương trình.
            }
        }

        /// <summary>
        /// Xây dựng chuỗi HTML chứa danh sách sản phẩm cho email từ giỏ hàng.
        /// </summary>  
        private async Task<string> BuildOrderItemsHtml(List<CartItem> cartItems)
        {
            var htmlBuilder = new StringBuilder();
            foreach (var item in cartItems)
            {
                // Truy vấn lại thông tin đầy đủ của phiên bản (bao gồm các giá trị thuộc tính)
                // Chúng ta vẫn cần truy vấn lại để lấy tên của các thuộc tính (Attribute.Name)
                var variant = await _db.ProductVariants
                                       .Include(v => v.Product) 
                                       .Include(v => v.VariantValues)
                                           .ThenInclude(vv => vv.AttributeValue)
                                               .ThenInclude(av => av.Attribute) 
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(v => v.Id == item.VariantId);

                if (variant != null)
                {
                    // Lấy tên sản phẩm từ DB để đảm bảo chính xác nhất
                    var productName = variant.Product.Name;

                    // Lấy mô tả phiên bản từ DB
                    var variantDescriptionHtml = string.Join("<br/>",
                        variant.VariantValues.Select(vv =>
                            $"<small style='color: #6c757d;'><strong>{vv.AttributeValue.Attribute.Name}:</strong> {vv.AttributeValue.Value}</small>"
                        )
                    );
            
                    // Xây dựng thẻ <tr> cho sản phẩm này
                    htmlBuilder.Append($@"
                        <tr>
                            <td style='padding: 10px; border: 1px solid #dee2e6;'>
                                <strong>{productName}</strong><br/>
                                {variantDescriptionHtml}
                            </td>
                            <td style='padding: 10px; border: 1px solid #dee2e6; text-align: center; vertical-align: middle;'>{item.Quantity}</td>
                            <td style='padding: 10px; border: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>{item.SubTotal.ToString("N0")}đ</td>
                        </tr>");
                }
            }
            return htmlBuilder.ToString();
        }

    }
}