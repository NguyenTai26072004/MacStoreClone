using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _db;

        public CategoryController(AppDbContext db)
        {
            _db = db;
        }


        public IActionResult Index(int id)
        {
            // BƯỚC 1: TÌM DANH MỤC ĐANG XEM
            // Dùng .Include() để lấy luôn thông tin của Cha (nếu có)
            var currentCategory = _db.Categories.Include(c => c.Parent).FirstOrDefault(c => c.Id == id);
            if (currentCategory == null)
            {
                return NotFound(); // Trả về lỗi 404 nếu không tìm thấy ID
            }

            // BƯỚC 2: TÌM RA "ĐẦU TÀU" CỦA BỘ LỌC
            // Nếu danh mục hiện tại có cha -> "đầu tàu" là cha của nó.
            // Nếu không -> "đầu tàu" là chính nó.
            var parentCategoryForFilter = currentCategory.Parent ?? currentCategory;

            // BƯỚC 3: LẤY DANH SÁCH CÁC MỤC CẦN HIỂN THỊ TRÊN THANH FILTER
            // Đây là danh sách tất cả các "anh em" của danh mục hiện tại.
            var filterCategories = _db.Categories
                .Where(c => c.ParentId == parentCategoryForFilter.Id)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            // BƯỚC 4: LẤY DANH SÁCH SẢN PHẨM CẦN HIỂN THỊ
            List<Product> products;
            if (currentCategory.ParentId == null) // Nếu đang xem danh mục cha (ví dụ: MacBook)
            {
                // Lấy ID của chính nó và tất cả các con của nó
                var categoryIds = new List<int> { currentCategory.Id };
                categoryIds.AddRange(filterCategories.Select(c => c.Id));

                // Lấy sản phẩm của TẤT CẢ các danh mục đó
                products = _db.Products
                    .Where(p => categoryIds.Contains(p.CategoryId) && p.IsPublished)
                    .Include(p => p.Images) 
                    .Include(p => p.Specifications.OrderBy(s => s.DisplayOrder))
                    .Include(p => p.Variants)
                    .ToList();
            }
            else // Nếu đang xem danh mục con (ví dụ: MacBook Air)
            {
                // Thì CHỈ lấy sản phẩm của chính nó mà thôi
                products = _db.Products
                    .Where(p => p.CategoryId == currentCategory.Id && p.IsPublished)
                    .Include(p => p.Images)
                    .Include(p => p.Specifications.OrderBy(s => s.DisplayOrder))
                    .Include(p => p.Variants)
                    .ToList();
            }

            // BƯỚC 5 (PHỤ): LẤY CÁC ICON ĐẦU TRANG
            var rootCategoriesForIcons = _db.Categories.Where(c => c.ParentId == null).OrderBy(c => c.DisplayOrder).ToList();

            // BƯỚC 6: GÓI MỌI THỨ VÀO VIEWMODEL VÀ GỬI ĐI
            var viewModel = new CategoryPageVM
            {
                MainCategory = currentCategory,
                SubCategories = filterCategories,
                Products = products,
                RootCategories = rootCategoriesForIcons,
                ParentCategoryForFilter = parentCategoryForFilter,
            };

            return View(viewModel);
        }
    }
}