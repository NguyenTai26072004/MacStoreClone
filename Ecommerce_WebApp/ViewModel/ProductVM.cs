// File: ViewModels/ProductVM.cs
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Ecommerce_WebApp.ViewModels
{
    public class ProductVM
    {
        // Dùng để binding dữ liệu cho sản phẩm chính
        public Product Product { get; set; }

        // Dùng để chứa danh sách lựa chọn cho dropdown Category
        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; }

        // Dùng để nhận các file ảnh từ form
        [ValidateNever]
        public List<IFormFile> Images { get; set; }

        [ValidateNever]
        public List<ProductAttribute> AttributeList { get; set; }
    }
}