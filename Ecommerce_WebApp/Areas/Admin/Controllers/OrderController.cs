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

        public OrderController(AppDbContext db)
        {
            _db = db;
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
        public async Task<IActionResult> ProcessOrder(int Id) // Id sẽ được binding từ <input asp-for="Id" hidden />
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

            orderHeader.OrderStatus = SD.OrderStatusShipped;
            orderHeader.TrackingNumber = trackingNumber; // Lưu mã vận đơn
            orderHeader.Carrier = carrier; // Lưu nhà vận chuyển

            await _db.SaveChangesAsync();

            // (Tùy chọn) Gửi email thông báo cho khách hàng rằng đơn hàng đã được giao

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
            // Vì là đơn COD, nên khi hoàn tất ta cũng cập nhật trạng thái thanh toán
            orderHeader.PaymentStatus = SD.PaymentStatusPaid;

            await _db.SaveChangesAsync();

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

            // Logic hoàn trả lại số lượng tồn kho
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

            TempData["info"] = "Đơn hàng đã được hủy."; 
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