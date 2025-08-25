using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_WebApp.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }

        // Thông tin người dùng (nếu đã đăng nhập)
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } // Ngày đặt hàng

        // --- THÔNG TIN NGƯỜI NHẬN HÀNG ---
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thành phố.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên quận/huyện.")]
        public string District { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phường/xã.")]
        public string Ward { get; set; }

        public string? OrderNotes { get; set; } 


        // --- THÔNG TIN ĐƠN HÀNG ---
        [Required]
        public decimal OrderTotal { get; set; }

        // Các trạng thái của đơn hàng
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }

        // Thông tin thanh toán và vận chuyển
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        // Chi tiết các sản phẩm trong đơn hàng này
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}