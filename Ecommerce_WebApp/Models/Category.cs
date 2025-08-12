using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Ecommerce_WebApp.Models
{
    public class Category
    {
        // Constructor để khởi tạo danh sách Children, tránh lỗi null
        public Category()
        {
            Children = new HashSet<Category>();
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        [MaxLength(100)]
        [DisplayName("Tên Danh mục")]
        public string Name { get; set; }

        [DisplayName("Thứ tự hiển thị")]
        public int? DisplayOrder { get; set; }

       
        [DisplayName("Danh mục cha")]
        public int? ParentId { get; set; }

        // Để lấy toàn bộ thông tin của danh mục cha khi cần
        [ForeignKey("ParentId")]
        [ValidateNever]
        public virtual Category Parent { get; set; }

        // Để lấy toàn bộ thông tin của danh mục con của 1 danh mục cha cụ thể khi cần
        [ValidateNever]
        public virtual ICollection<Category> Children { get; set; }


        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    }
}