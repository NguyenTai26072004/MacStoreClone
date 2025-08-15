using Ecommerce_WebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;

        public CategoryMenuViewComponent(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy tất cả các danh mục là danh mục GỐC (không có cha)
            // Sắp xếp chúng theo thứ tự hiển thị
            // Và .Include() cả danh sách các danh mục CON của chúng (cũng được sắp xếp)
            var categories = await _db.Categories
                                      .Where(c => c.ParentId == null)
                                      .OrderBy(c => c.DisplayOrder)
                                      .Include(c => c.Children.OrderBy(child => child.DisplayOrder))
                                      .ToListAsync();

            // Trả về View của Component này và gửi kèm danh sách đã lấy được
            return View(categories);
        }
    }
}