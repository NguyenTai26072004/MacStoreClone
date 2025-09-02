using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;



        public OrderController(AppDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        // GET: Hiển thị danh sách tất cả đơn hàng
        public async Task<IActionResult> Index(string status)
        {
            var query = _db.OrderHeaders.AsQueryable();

            // Áp dụng bộ lọc trạng thái nếu có
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(oh => oh.OrderStatus == status);
            }

            var orders = await query.OrderByDescending(oh => oh.OrderDate).ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int orderId)
        {
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

            if (orderHeader == null)
            {
                return NotFound();
            }

            return View(orderHeader);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(int Id) 
        {
            var orderHeader = await _db.OrderHeaders.FindAsync(Id);
            if (orderHeader == null) return NotFound();

            orderHeader.OrderStatus = SD.OrderStatusProcessing;
            await _db.SaveChangesAsync();

            TempData["success"] = "Đơn hàng đã được chuyển sang trạng thái 'Đang xử lý'.";
            return RedirectToAction("Details", new { orderId = Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int Id, string trackingNumber, string carrier)
        {
            var orderHeader = await _db.OrderHeaders.FindAsync(Id);
            if (orderHeader == null) return NotFound();

            // Cập nhật thông tin đơn hàng
            orderHeader.OrderStatus = SD.OrderStatusShipped;
            orderHeader.TrackingNumber = trackingNumber;
            orderHeader.Carrier = carrier;

            await _db.SaveChangesAsync();

            // === GỌI HÀM GỬI EMAIL THÔNG BÁO GIAO HÀNG ===
            try
            {
                string subject = $"Đơn hàng #{orderHeader.Id} của bạn đã được giao";
                var replacements = new Dictionary<string, string>
                {
                    { "{{CustomerName}}", orderHeader.FullName },
                    { "{{OrderId}}", orderHeader.Id.ToString() },
                    { "{{Carrier}}", orderHeader.Carrier },
                    { "{{TrackingNumber}}", orderHeader.TrackingNumber }
                };

                await _emailSender.SendEmailFromTemplateAsync(orderHeader.Email, subject, "OrderShipped.html", replacements);
            }
            catch (Exception)
            {
                // Ghi log lỗi nếu cần, nhưng không làm crash chương trình
                // Việc kinh doanh quan trọng hơn việc gửi email
            }


            TempData["success"] = "Đơn hàng đã được giao cho đơn vị vận chuyển.";
            return RedirectToAction("Details", new { orderId = Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int Id)
        {
            var orderHeader = await _db.OrderHeaders.FindAsync(Id);
            if (orderHeader == null) return NotFound();

            orderHeader.OrderStatus = SD.OrderStatusCompleted;
            orderHeader.PaymentStatus = SD.PaymentStatusPaid;

            await _db.SaveChangesAsync();

            // === GỌI HÀM GỬI EMAIL THÔNG BÁO HOÀN THÀNH ===
            try
            {
                string subject = $"Đơn hàng #{orderHeader.Id} của bạn đã hoàn thành";
                var replacements = new Dictionary<string, string>
                {
                    { "{{CustomerName}}", orderHeader.FullName },
                    { "{{OrderId}}", orderHeader.Id.ToString() }
                };

                // Gọi service và chỉ định đúng tên template mới
                await _emailSender.SendEmailFromTemplateAsync(orderHeader.Email, subject, "OrderCompleted.html", replacements);
            }
            catch (Exception)
            {
                // Ghi log lỗi nhưng không làm ảnh hưởng đến luồng chính
            }
            // ===========================================

            TempData["success"] = "Đơn hàng đã được hoàn tất.";
            return RedirectToAction("Details", new { orderId = Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int Id)
        {
            var orderHeader = await _db.OrderHeaders
                                    .Include(oh => oh.OrderDetails)
                                    .FirstOrDefaultAsync(oh => oh.Id == Id);

            if (orderHeader == null) return NotFound();

            // Logic hoàn trả lại số lượng tồn kho (giữ nguyên)
            foreach (var detail in orderHeader.OrderDetails)
            {
                var variant = await _db.ProductVariants.FindAsync(detail.ProductVariantId);
                if (variant != null)
                {
                    variant.StockQuantity += detail.Quantity;
                }
            }

            orderHeader.OrderStatus = SD.OrderStatusCancelled;

            await _db.SaveChangesAsync();

            // === GỌI HÀM GỬI EMAIL THÔNG BÁO HỦY ĐƠN ===
            try
            {
                string subject = $"Thông báo về đơn hàng #{orderHeader.Id}";
                var replacements = new Dictionary<string, string>
            {
                { "{{CustomerName}}", orderHeader.FullName },
                { "{{OrderId}}", orderHeader.Id.ToString() }
            };

                await _emailSender.SendEmailFromTemplateAsync(orderHeader.Email, subject, "OrderCancelled.html", replacements);
            }
            catch (Exception)
            {
                // Ghi log
            }
            // ===========================================

            TempData["info"] = "Đơn hàng đã được hủy thành công.";
            return RedirectToAction("Details", new { orderId = Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int Id)
        {
            // Tìm đơn hàng cần xóa
            var orderHeaderToDelete = await _db.OrderHeaders.FindAsync(Id);

            if (orderHeaderToDelete == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng để xóa.";
                return RedirectToAction("Index");
            }

            // Chỉ cho phép xóa các đơn hàng đã bị hủy
            if (orderHeaderToDelete.OrderStatus != SD.OrderStatusCancelled)
            {
                TempData["error"] = "Chỉ có thể xóa các đơn hàng đã bị hủy.";
                return RedirectToAction("Details", new { orderId = Id });
            }


            _db.OrderHeaders.Remove(orderHeaderToDelete);
            await _db.SaveChangesAsync();

            TempData["success"] = $"Đã xóa vĩnh viễn đơn hàng #{Id}.";
            return RedirectToAction("Index");
        }
    }
}