// File: Models/AttributeValue.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_WebApp.Models
{
    public class AttributeValue
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại trỏ đến bảng Attribute
        public int AttributeId { get; set; }
        [ForeignKey("AttributeId")]
        [ValidateNever]
        public virtual ProductAttribute Attribute { get; set; }

        [Required]
        [MaxLength(100)]
        public string Value { get; set; } // Ví dụ: "Bạc", "Xám không gian", "16GB", "512GB"
    }
}