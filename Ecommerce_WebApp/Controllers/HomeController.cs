using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ecommerce_WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Bước 1: Vẫn lấy tất cả dữ liệu cần thiết
            var allProducts = _db.Products
                .Include(p => p.Category).ThenInclude(c => c.Parent)
                .Include(p => p.Images.Where(i => i.IsPrimary))
                .Include(p => p.Specifications.OrderBy(s => s.DisplayOrder).Take(4))
                .Include(p => p.Variants)
                .ToList();

            var featuredCategoryNames = new List<string> { "MacBook", "iMac", "Mac Studio", "Mac Mini", "Phụ kiện" };
            var featuredCategories = _db.Categories
                                        .Where(c => featuredCategoryNames.Contains(c.Name))
                                        .OrderBy(c => c.DisplayOrder)
                                        .ToList();

            // Bước 3: Tạo ViewModel với logic lọc đã được sửa lỗi
            var homeVM = new HomeVM
            {
                NewProducts = allProducts.OrderByDescending(p => p.Id).Take(8),

                // SỬ DỤNG LOGIC LỌC LINH HOẠT HƠN
                MacBookProducts = allProducts
                    .Where(p => p.Category?.Name == "MacBook" || p.Category?.Parent?.Name == "MacBook")
                    .Take(4).ToList(),

                IMacProducts = allProducts
                    .Where(p => p.Category?.Name == "iMac" || p.Category?.Parent?.Name == "iMac")
                    .Take(4).ToList(),

                MacStudioProducts = allProducts
                    .Where(p => p.Category?.Name == "Mac Studio" || p.Category?.Parent?.Name == "Mac Studio")
                    .Take(4).ToList(),

                MacMiniProducts = allProducts
                    .Where(p => p.Category?.Name == "Mac Mini" || p.Category?.Parent?.Name == "Mac Mini")
                    .Take(4).ToList(),

                PhuKienProducts = allProducts
                    .Where(p => p.Category?.Name == "Phụ Kiện" || p.Category?.Parent?.Name == "Phụ Kiện")
                    .Take(4).ToList(),

                FeaturedCategories = featuredCategories
            };

            return View(homeVM);
        }

    }
}