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
        private readonly IConfiguration _config;
        private readonly ILogger<OrderController> _logger;

        public OrderController(AppDbContext db, Utility.IEmailSender emailSender, IMomoService momoService, IConfiguration config, ILogger<OrderController> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _momoService = momoService;
            _config = config;
            _logger = logger;
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


        public async Task<IActionResult> PaymentCallBack()
        {
            try
            {
                var query = HttpContext.Request.Query;

                // 1. Lấy tất cả tham số từ MoMo callback
                var partnerCode = query["partnerCode"].ToString();
                var orderId = query["orderId"].ToString();
                var requestId = query["requestId"].ToString();
                var amount = query["amount"].ToString();
                var orderInfo = query["orderInfo"].ToString();
                var orderType = query["orderType"].ToString();
                var transId = query["transId"].ToString();
                var resultCode = query["resultCode"].ToString();
                var message = query["message"].ToString();
                var payType = query["payType"].ToString();
                var responseTime = query["responseTime"].ToString();
                var extraData = query["extraData"].ToString();
                var signature = query["signature"].ToString();

                _logger.LogInformation($"MoMo callback received: resultCode={resultCode}, extraData={extraData}");

                // 2. Verify signature để đảm bảo request từ MoMo (QUAN TRỌNG!)
                var secretKey = _config["MomoSettings:SecretKey"];
                var accessKey = _config["MomoSettings:AccessKey"];
                var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

                MoMoSecurity crypto = new MoMoSecurity();
                var computedSignature = crypto.signSHA256(rawHash, secretKey);

                if (computedSignature != signature)
                {
                    _logger.LogWarning("Invalid MoMo signature detected. Possible fraud attempt.");
                    TempData["error"] = "Chữ ký không hợp lệ. Vui lòng liên hệ hỗ trợ.";
                    return RedirectToAction("Index", "Cart");
                }

                // 3. Xử lý đơn hàng với transaction để tránh race condition
                if (int.TryParse(extraData, out int dbOrderId))
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        var orderHeader = await _db.OrderHeaders.FindAsync(dbOrderId);
                        if (orderHeader == null)
                        {
                            _logger.LogError($"Order #{dbOrderId} not found in database");
                            TempData["error"] = "Không tìm thấy đơn hàng.";
                            return RedirectToAction("Index", "Cart");
                        }

                        if (resultCode == "0") // Giao dịch THÀNH CÔNG
                        {
                            if (orderHeader.PaymentStatus != SD.PaymentStatusPaid)
                            {
                                // Cập nhật trạng thái
                                orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                                orderHeader.OrderStatus = SD.OrderStatusProcessing;
                                _db.OrderHeaders.Update(orderHeader);
                                await _db.SaveChangesAsync();

                                // Trừ kho, gửi mail, xóa giỏ hàng
                                await FinalizeOrder(orderHeader);
                                await transaction.CommitAsync();

                                _logger.LogInformation($"Order #{dbOrderId} payment confirmed successfully");
                            }
                            return RedirectToAction("ThankYou", new { orderId = orderHeader.Id });
                        }
                        else // Giao dịch THẤT BẠI
                        {
                            _logger.LogWarning($"Order #{dbOrderId} payment failed: {message}");
                            _db.OrderHeaders.Remove(orderHeader);
                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            TempData["error"] = $"Thanh toán thất bại: {message}";
                            return RedirectToAction("Index", "Cart");
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error processing MoMo callback");
                        throw;
                    }
                }

                TempData["error"] = "Dữ liệu callback không hợp lệ.";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PaymentCallBack");
                TempData["error"] = "Có lỗi xảy ra. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index", "Cart");
            }
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
        /// Xử lý IPN (Instant Payment Notification) từ MoMo server-to-server
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PaymentIPN()
        {
            try
            {
                // IPN nhận POST data thay vì query string
                var form = HttpContext.Request.Form;

                var partnerCode = form["partnerCode"].ToString();
                var orderId = form["orderId"].ToString();
                var requestId = form["requestId"].ToString();
                var amount = form["amount"].ToString();
                var orderInfo = form["orderInfo"].ToString();
                var orderType = form["orderType"].ToString();
                var transId = form["transId"].ToString();
                var resultCode = form["resultCode"].ToString();
                var message = form["message"].ToString();
                var payType = form["payType"].ToString();
                var responseTime = form["responseTime"].ToString();
                var extraData = form["extraData"].ToString();
                var signature = form["signature"].ToString();

                _logger.LogInformation($"MoMo IPN received: resultCode={resultCode}, extraData={extraData}");

                // Verify signature
                var secretKey = _config["MomoSettings:SecretKey"];
                var accessKey = _config["MomoSettings:AccessKey"];
                var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

                MoMoSecurity crypto = new MoMoSecurity();
                var computedSignature = crypto.signSHA256(rawHash, secretKey);

                if (computedSignature != signature)
                {
                    _logger.LogWarning("Invalid MoMo IPN signature");
                    return StatusCode(403, new { message = "Invalid signature" });
                }

                // Xử lý đơn hàng (tương tự PaymentCallBack)
                if (int.TryParse(extraData, out int dbOrderId))
                {
                    var orderHeader = await _db.OrderHeaders.FindAsync(dbOrderId);
                    if (orderHeader != null && resultCode == "0" && orderHeader.PaymentStatus != SD.PaymentStatusPaid)
                    {
                        using var transaction = await _db.Database.BeginTransactionAsync();
                        try
                        {
                            orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                            orderHeader.OrderStatus = SD.OrderStatusProcessing;
                            _db.OrderHeaders.Update(orderHeader);
                            await _db.SaveChangesAsync();

                            await FinalizeOrder(orderHeader);
                            await transaction.CommitAsync();

                            _logger.LogInformation($"Order #{dbOrderId} confirmed via IPN");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Error processing IPN");
                            return StatusCode(500, new { message = "Internal server error" });
                        }
                    }
                }

                // MoMo yêu cầu response 200 OK
                return Ok(new { message = "IPN processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo IPN");
                return StatusCode(500, new { message = "Internal server error" });
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