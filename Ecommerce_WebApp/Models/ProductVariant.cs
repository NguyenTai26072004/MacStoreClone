// File: Models/ProductVariant.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_WebApp.Models
{
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại trỏ về Sản phẩm gốc
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [MaxLength(100)]
        public string Sku { get; set; } // Mã SKU duy nhất cho phiên bản này

        [Required]
        public decimal Price { get; set; } // Giá của phiên bản này

        public int StockQuantity { get; set; } // Số lượng tồn kho

        // Thuộc tính điều hướng: Một phiên bản được tạo thành từ nhiều giá trị thuộc tính
        public virtual ICollection<VariantValue> VariantValues { get; set; }
    }
}