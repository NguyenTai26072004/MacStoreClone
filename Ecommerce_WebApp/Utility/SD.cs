// File: Utility/SD.cs
namespace Ecommerce_WebApp.Utility
{
    public static class SD // SD = Static Details
    {
        // Trạng thái đơn hàng
        public const string OrderStatusPending = "Pending";       // Chờ xử lý
        public const string OrderStatusProcessing = "Processing"; // Đang xử lý
        public const string OrderStatusShipped = "Shipped";       // Đã giao hàng
        public const string OrderStatusCompleted = "Completed";   // Hoàn tất
        public const string OrderStatusCancelled = "Cancelled";   // Đã hủy

        // Trạng thái thanh toán
        public const string PaymentStatusPending = "Pending";      // Chưa thanh toán (COD)
        public const string PaymentStatusPaid = "Paid";          // Đã thanh toán (Online)


        // Phương thức thanh toán
        public const string PaymentMethodCOD = "COD";            // Thanh toán khi nhận hàng
        public const string PaymentMethodMomo = "Momo";          // Thanh toán qua Momo
        public const string PaymentMethodBank = "BankTransfer";  // Chuyển khoản ngân hàng
    }
}