using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Helpers;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.Services;
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
        private readonly IMomoService _momoService;

        public OrderController(AppDbContext db, Utility.IEmailSender emailSender, IMomoService momoService)
        {
            _db = db;
            _emailSender = emailSender;
            _momoService = momoService;
        }

        // GET: Hiển thị trang Checkout
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

        // --- ACTION CHÍNH: XỬ LÝ ĐẶT HÀNG ---
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

            LinkUserToOrderHeader(viewModel.OrderHeader);

            ModelState.Remove("OrderHeader.ApplicationUser");
            if (ModelState.IsValid)
            {
                // Bước 1: Tạo và Lưu Đơn hàng ở trạng thái Pending (Chưa xử lý)
                // Áp dụng cho cả COD và MoMo.
                await CreateAndSaveOrderHeaderAndDetails(viewModel);

                // Bước 2: Phân nhánh xử lý theo phương thức thanh toán
                if (viewModel.PaymentMethod == SD.PaymentMethodMomo)
                {
                    // Chuyển hướng đến MoMo để thanh toán
                    string paymentUrl = await _momoService.CreatePaymentUrlAsync(viewModel.OrderHeader, HttpContext);
                    if (!string.IsNullOrEmpty(paymentUrl))
                    {
                        return Redirect(paymentUrl);
                    }
                    else
                    {
                        TempData["error"] = "Không thể tạo yêu cầu thanh toán MoMo. Vui lòng thử lại sau.";
                        return View(viewModel); // Lỗi, quay lại trang checkout
                    }
                }
                else // Mặc định là COD
                {
                    // Với COD, chúng ta có thể trừ kho và gửi email ngay
                    await FinalizeOrder(viewModel.OrderHeader);
                    return RedirectToAction("ThankYou", new { orderId = viewModel.OrderHeader.Id });
                }
            }

            return View(viewModel); // Lỗi validation, quay lại trang checkout
        }


        // --- ACTION XỬ LÝ PHẢN HỒI TỪ MOMO ---
        public async Task<IActionResult> PaymentCallBack()
        {
            var query = HttpContext.Request.Query;
            var extraData = query["extraData"].ToString();

            if (int.TryParse(extraData, out int dbOrderId))
            {
                var orderHeader = await _db.OrderHeaders.FindAsync(dbOrderId);
                if (orderHeader == null)
                {
                    TempData["error"] = "Không tìm thấy đơn hàng trong hệ thống.";
                    return RedirectToAction("Index", "Cart");
                }

                var resultCode = query["resultCode"].ToString();
                if (resultCode == "0") // Giao dịch thành công
                {
                    // Đánh dấu đơn hàng là đã thanh toán
                    orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                    _db.OrderHeaders.Update(orderHeader);
                    await _db.SaveChangesAsync();

                    // Hoàn tất đơn hàng (trừ kho, gửi mail)
                    await FinalizeOrder(orderHeader);

                    return RedirectToAction("ThankYou", new { orderId = orderHeader.Id });
                }
            }

            TempData["error"] = $"Thanh toán MoMo thất bại: {query["message"]}";
            // Cập nhật trạng thái đơn hàng là "thanh toán thất bại"
            // (Bạn có thể thêm logic này nếu muốn)
            return RedirectToAction("Index", "Cart");
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
        /// Tạo và lưu OrderHeader & OrderDetail, trả về OrderHeader đã được lưu
        /// </summary>
        private async Task CreateAndSaveOrderHeaderAndDetails(CheckoutVM viewModel)
        {
            var orderHeader = viewModel.OrderHeader;
            orderHeader.OrderDate = DateTime.Now;
            orderHeader.OrderTotal = viewModel.ShoppingCart.Items.Sum(i => i.SubTotal);
            orderHeader.OrderStatus = SD.OrderStatusPending;
            orderHeader.PaymentStatus = SD.PaymentStatusPending;
            orderHeader.PaymentMethod = viewModel.PaymentMethod;
            _db.OrderHeaders.Add(orderHeader);
            await _db.SaveChangesAsync();

            var orderDetails = viewModel.ShoppingCart.Items.Select(item => new OrderDetail
            {
                OrderHeaderId = orderHeader.Id,
                ProductVariantId = item.VariantId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();
            _db.OrderDetails.AddRange(orderDetails);
            await _db.SaveChangesAsync();

            // Gán lại OrderDetails để các hàm sau có thể dùng
            orderHeader.OrderDetails = orderDetails;
        }

        /// <summary>
        /// Hoàn tất đơn hàng: Trừ kho, gửi mail, xóa giỏ hàng
        /// </summary>
        private async Task FinalizeOrder(OrderHeader orderHeader)
        {
            // 1. Trừ kho
            var orderDetails = _db.OrderDetails.Where(od => od.OrderHeaderId == orderHeader.Id).ToList();
            foreach (var item in orderDetails)
            {
                var variantInDb = await _db.ProductVariants.FindAsync(item.ProductVariantId);
                if (variantInDb != null)
                {
                    variantInDb.StockQuantity -= item.Quantity;
                }
            }
            await _db.SaveChangesAsync();

            // 2. Gửi email
            await SendConfirmationEmail(orderHeader);

            // 3. Xóa giỏ hàng
            HttpContext.Session.Remove(CartController.SessionKey);
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
        private async Task SendConfirmationEmail(OrderHeader orderHeader)
        {
            try
            {
                // Đảm bảo OrderDetails đã được tải đầy đủ thông tin cần thiết
                if (orderHeader.OrderDetails == null || !orderHeader.OrderDetails.Any())
                {
                    orderHeader.OrderDetails = await _db.OrderDetails
                        .Where(od => od.OrderHeaderId == orderHeader.Id)
                        .ToListAsync();
                }

                string subject = $"MacStore - Xác nhận đơn hàng #{orderHeader.Id}";

                // Gọi hàm BuildOrderItemsHtml với danh sách OrderDetails
                string orderItemsHtml = await BuildOrderItemsHtml(orderHeader.OrderDetails);

                var replacements = new Dictionary<string, string>
                {
                    { "{{CustomerName}}", orderHeader.FullName },
                    { "{{OrderId}}", orderHeader.Id.ToString() },
                    { "{{OrderItems}}", orderItemsHtml },
                    { "{{OrderTotal}}", orderHeader.OrderTotal.ToString("N0") },
                    { "{{ShippingAddress}}", $"{orderHeader.Address}, {orderHeader.Ward}, {orderHeader.District}, {orderHeader.City}" }
                };

                await _emailSender.SendEmailFromTemplateAsync(orderHeader.Email, subject, "OrderConfirmation.html", replacements);
            }
            catch (Exception)
            {
                // Ghi log lỗi nhưng không làm crash chương trình.
            }
        }


        /// <summary>
        /// Xây dựng chuỗi HTML chứa danh sách sản phẩm cho email từ danh sách OrderDetail.
        /// </summary>
        private async Task<string> BuildOrderItemsHtml(List<OrderDetail> orderDetails)
        {
            var htmlBuilder = new StringBuilder();
            foreach (var detail in orderDetails)
            {
                // Truy vấn thông tin của phiên bản sản phẩm tương ứng
                var variant = await _db.ProductVariants
                                       .Include(v => v.Product)
                                       .Include(v => v.VariantValues)
                                           .ThenInclude(vv => vv.AttributeValue)
                                               .ThenInclude(av => av.Attribute)
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(v => v.Id == detail.ProductVariantId);

                if (variant != null)
                {
                    var productName = variant.Product.Name;

                    // Lấy mô tả phiên bản
                    var variantDescriptionHtml = string.Join("<br/>",
                        variant.VariantValues.Select(vv =>
                            $"<small style='color: #6c757d;'><strong>{vv.AttributeValue.Attribute.Name}:</strong> {vv.AttributeValue.Value}</small>"
                        )
                    );

                    var subTotal = detail.Quantity * detail.Price;

                    htmlBuilder.Append($@"
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>
                        <strong>{productName}</strong><br/>
                        {variantDescriptionHtml}
                    </td>
                    <td style='padding: 10px; border: 1px solid #dee2e6; text-align: center; vertical-align: middle;'>{detail.Quantity}</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>{subTotal.ToString("N0")}đ</td>
                </tr>");
                }
            }
            return htmlBuilder.ToString();
        }

    }
}