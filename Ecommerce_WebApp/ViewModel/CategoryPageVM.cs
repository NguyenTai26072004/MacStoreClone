using Ecommerce_WebApp.Models;
using System.Collections.Generic;

namespace Ecommerce_WebApp.ViewModels
{
    public class CategoryPageVM
    {

        public Category MainCategory { get; set; }

        public IEnumerable<Product> Products { get; set; }

        public IEnumerable<Category> SubCategories { get; set; }

        public IEnumerable<Category> RootCategories { get; set; }

        public Category ParentCategoryForFilter { get; set; }

        public decimal? MaxPriceSelected { get; set; } // Giá tối đa người dùng đã chọn
        public decimal MinPriceAvailable { get; set; }  // Giá thấp nhất trong danh mục
        public decimal MaxPriceAvailable { get; set; }  // Giá cao nhất trong danh mục
    }
}
