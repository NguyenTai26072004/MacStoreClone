using Ecommerce_WebApp.Models;
using System.Collections.Generic;

namespace Ecommerce_WebApp.ViewModels
{
    public class ProductDetailVM
    {
        // Sản phẩm chính đang được xem
        public Product Product { get; set; }

        // Danh sách các sản phẩm tương tự
        public IEnumerable<Product> RelatedProducts { get; set; }
    }
}