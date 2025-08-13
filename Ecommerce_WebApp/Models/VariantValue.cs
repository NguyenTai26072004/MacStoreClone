// File: Models/VariantValue.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_WebApp.Models
{
    // Bảng này không cần Id riêng, nó dùng khóa chính kết hợp (composite key)
    public class VariantValue
    {
        // Khóa ngoại trỏ đến Phiên bản sản phẩm
        public int ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        // Khóa ngoại trỏ đến Giá trị thuộc tính
        public int AttributeValueId { get; set; }
        [ForeignKey("AttributeValueId")]
        public virtual AttributeValue AttributeValue { get; set; }
    }
}