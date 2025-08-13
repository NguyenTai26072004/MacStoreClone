// File: Models/Product.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Ecommerce_WebApp.Models
{
    public class Product
    {
        public Product()
        {
            // Khởi tạo các danh sách để tránh lỗi null
            Images = new HashSet<ProductImage>();
            Variants = new HashSet<ProductVariant>();
            Specifications = new HashSet<ProductSpecification>();
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [MaxLength(255)]
        [DisplayName("Tên sản phẩm")]
        public string Name { get; set; } // Tên chung: "MacBook Pro 14 inch 2024"

        [Required(ErrorMessage = "Mô tả không được để trống.")]
        [DisplayName("Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [DisplayName("Danh mục")]
        public int CategoryId { get; set; }

        [ValidateNever]
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public bool IsPublished { get; set; } = true; // Dùng để ẩn/hiện sản phẩm

        // --- Thuộc tính điều hướng đến các bảng con ---
        [ValidateNever]
        public virtual ICollection<ProductImage> Images { get; set; }
        [ValidateNever]
        public virtual ICollection<ProductVariant> Variants { get; set; }
        [ValidateNever]
        public virtual ICollection<ProductSpecification> Specifications { get; set; }
    }
}