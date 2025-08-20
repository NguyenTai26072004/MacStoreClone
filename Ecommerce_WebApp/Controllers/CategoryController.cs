using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Cần dùng async để truy vấn hiệu quả

namespace Ecommerce_WebApp.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _db;

        public CategoryController(AppDbContext db)
        {
            _db = db;
        }

        
        public async Task<IActionResult> Index(int id, decimal? maxPrice = null)
        {
            
            var currentCategory = await _db.Categories.AsNoTracking().Include(c => c.Parent).FirstOrDefaultAsync(c => c.Id == id);
            if (currentCategory == null) return NotFound();

            var parentCategoryForFilter = currentCategory.Parent ?? currentCategory;

            var filterCategories = await _db.Categories.AsNoTracking()
                .Where(c => c.ParentId == parentCategoryForFilter.Id)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var rootCategoriesForIcons = await _db.Categories.AsNoTracking().Where(c => c.ParentId == null).OrderBy(c => c.DisplayOrder).ToListAsync();


            var categoryIdsToLoad = new List<int> { id };
            if (currentCategory.ParentId == null)
            {
                categoryIdsToLoad.AddRange(filterCategories.Select(c => c.Id));
            }

            // 1. Tạo câu truy vấn ban đầu 
            var productsQuery = _db.Products
                .AsNoTracking()
                .Where(p => categoryIdsToLoad.Contains(p.CategoryId) && p.IsPublished);

            // 2. Tìm khoảng giá min/max từ TẤT CẢ sản phẩm trong nhóm
            var allVariantsInCategories = productsQuery.SelectMany(p => p.Variants);
            decimal minPriceAvailable = await allVariantsInCategories.AnyAsync() ? await allVariantsInCategories.MinAsync(v => v.Price) : 0;
            decimal maxPriceAvailable = await allVariantsInCategories.AnyAsync() ? await allVariantsInCategories.MaxAsync(v => v.Price) : 0;

            // 3. Áp dụng bộ lọc giá (nếu người dùng đã chọn)
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Variants.Any(v => v.Price <= maxPrice.Value));
            }

            // 4. Lấy danh sách sản phẩm cuối cùng SAU KHI đã lọc
            var products = await productsQuery
                .OrderByDescending(p => p.Id)
                .Include(p => p.Images)
                .Include(p => p.Specifications.OrderBy(s => s.DisplayOrder))
                .Include(p => p.Variants)
                .ToListAsync();

            // --- TẠO VIEWMODEL HOÀN CHỈNH ---
            var viewModel = new CategoryPageVM
            {
                MainCategory = currentCategory,
                SubCategories = filterCategories,
                Products = products,
                RootCategories = rootCategoriesForIcons,
                ParentCategoryForFilter = parentCategoryForFilter,

                // Gửi các giá trị cho bộ lọc giá
                MaxPriceSelected = maxPrice,
                MinPriceAvailable = minPriceAvailable,
                MaxPriceAvailable = maxPriceAvailable > minPriceAvailable ? maxPriceAvailable : (minPriceAvailable + 10000000) // Đảm bảo max > min
            };

            return View(viewModel);
        }
    }
}