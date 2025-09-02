// File: ViewModels/CategoryFormVM.cs
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Ecommerce_WebApp.ViewModels
{
    public class CategoryFormVM
    {
        public Category Category { get; set; } = new Category();

        // Dùng cho việc upload ảnh icon MỚI
        public IFormFile? IconImage { get; set; }

        // Dùng để chứa danh sách chọn danh mục cha
        [ValidateNever]
        public IEnumerable<SelectListItem> ParentCategoryList { get; set; }
    }
}