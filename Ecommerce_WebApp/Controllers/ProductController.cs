using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ecommerce_WebApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _db;

        public ProductController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Product/Details/{id}
        public IActionResult Details(int id)
        {
            // Truy vấn để lấy sản phẩm đang xem và tất cả dữ liệu liên quan
            var product = _db.Products
                .Include(p => p.Category).ThenInclude(c => c.Parent) // Lấy cả cha và ông
                .Include(p => p.Images)
                .Include(p => p.Specifications.OrderBy(s => s.DisplayOrder))
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantValues)
                        .ThenInclude(vv => vv.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy 4 sản phẩm tương tự (cùng danh mục, trừ chính nó)
            var relatedProducts = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4)
                .ToList();

            // Tạo ViewModel
            var viewModel = new ProductDetailVM
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }
    }
}