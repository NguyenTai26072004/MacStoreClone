// File: Models/Attribute.cs
using System.ComponentModel.DataAnnotations;

namespace Ecommerce_WebApp.Models
{
    public class ProductAttribute
    {
        public ProductAttribute()
        {
            // Khởi tạo danh sách Values để nó không bao giờ bị null.
            // Điều này sẽ làm hài lòng hệ thống Model Validation.
            Values = new HashSet<AttributeValue>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // Ví dụ: "Màu sắc", "Dung lượng RAM"

        // Thuộc tính điều hướng: Một thuộc tính có nhiều giá trị có thể có
        public virtual ICollection<AttributeValue> Values { get; set; }
    }
}