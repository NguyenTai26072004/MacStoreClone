// File: Models/ProductImage.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_WebApp.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại trỏ về Sản phẩm gốc
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        public string ImageUrl { get; set; } // Đường dẫn đến file ảnh

        public bool IsPrimary { get; set; } // Đánh dấu đây có phải ảnh đại diện không
    }
}