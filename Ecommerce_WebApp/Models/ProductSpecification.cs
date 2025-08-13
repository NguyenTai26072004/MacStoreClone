// File: Models/ProductSpecification.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_WebApp.Models
{
    public class ProductSpecification
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại trỏ về Sản phẩm gốc
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } // Ví dụ: "Tình trạng", "CPU"

        [Required]
        [MaxLength(255)]
        public string Value { get; set; } // Ví dụ: "Mới 100%", "Apple M4 Pro chip..."

        public int DisplayOrder { get; set; } // Để sắp xếp thứ tự hiển thị các gạch đầu dòng
    }
}