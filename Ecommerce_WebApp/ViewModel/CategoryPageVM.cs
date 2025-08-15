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
    }
}
