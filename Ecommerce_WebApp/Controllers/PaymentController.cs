using Microsoft.AspNetCore.Mvc;

namespace YourProject.Controllers
{
    // Controller này thường hoạt động như một API endpoint
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        // Ghi chú: Inject các services cần thiết
        // private readonly IOrderRepository _orderRepository;
        // private readonly IPaymentService _paymentService;

        // [HttpGet("MomoCallback")]
        // Nơi MoMo/VNPAY trả người dùng về sau khi thanh toán xong
        //public IActionResult PaymentCallback([FromQuery] /*MomoResponseModel response*/)
        //{
        //    // Ghi chú:
        //    // 1. Lấy các tham số từ query string mà cổng thanh toán trả về.
        //    // 2. Gọi service để xác thực chữ ký (signature) và tính hợp lệ của phản hồi.
        //    // 3. Nếu hợp lệ và thanh toán thành công, cập nhật trạng thái đơn hàng trong DB thành "Paid".
        //    // 4. Chuyển hướng người dùng đến trang ThankYou.
        //    // 5. Nếu thất bại, chuyển hướng đến trang thông báo lỗi thanh toán.
        //    int orderId = 12345; // Lấy orderId từ response
        //    return RedirectToAction("ThankYou", "Order", new { orderId = orderId });
        //}

        //// [HttpPost("MomoWebhook")]
        //// Nơi MoMo/VNPAY gửi một request ngầm (server-to-server) để thông báo trạng thái giao dịch
        //public IActionResult WebhookNotification([FromBody] /*MomoWebhookModel model*/)
        //{
        //    // Ghi chú:
        //    // 1. Đây là cách xác nhận thanh toán đáng tin cậy nhất.
        //    // 2. Xác thực chữ ký hoặc mã bí mật từ request.
        //    // 3. Cập nhật trạng thái đơn hàng trong database.
        //    // 4. Trả về một mã trạng thái HTTP 200 OK để báo cho cổng thanh toán biết đã nhận được thông báo.
        //    return Ok();
        //}
    }
}