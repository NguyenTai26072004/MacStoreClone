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
            // Truy vấn cơ sở dữ liệu để lấy danh sách sản phẩm
            var allProducts = _db.Products
                .Include(p => p.Category)         
                .Include(p => p.Images)           
                .Include(p => p.Specifications)  
                .Include(p => p.Variants)         
                .Where(p => p.IsPublished)        
                .ToList();

            // Tạo ViewModel và đổ dữ liệu đã lấy vào
            var homeVM = new HomeVM
            {
                // Lấy 4 sản phẩm mới nhất
                NewProducts = allProducts.OrderByDescending(p => p.Id).Take(4),

                // Lấy 4 sản phẩm thuộc danh mục "MacBook"
                MacBookProducts = allProducts.Where(p => p.Category.Name == "MacBook M1").Take(4),

                // Lấy 4 sản phẩm thuộc danh mục "iMac"
                IMacProducts = allProducts.Where(p => p.Category.Name == "iMac").Take(4),

                // Lấy 4 sản phẩm thuộc danh mục "Mac Studio"
                MacStudioProducts = allProducts.Where(p => p.Category.Name == "Mac Studio").Take(4),

                // Lấy 4 sản phẩm thuộc danh mục "Mac Mini"
                MacMiniProducts = allProducts.Where(p => p.Category.Name == "Mac Mini").Take(4),

                // Lấy 4 sản phẩm thuộc danh mục "Phụ kiện"
                PhuKien = allProducts.Where(p => p.Category.Name == "Phụ kiện").Take(4),


            };


            return View(homeVM);
        }

    }
}