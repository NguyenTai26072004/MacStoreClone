using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Linq;
using System.Threading.Tasks; // Thêm using cho Task (async)

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductAttributeController : Controller
    {
        private readonly AppDbContext _db;

        public ProductAttributeController(AppDbContext db)
        {
            _db = db;
        }

        #region READ ACTIONS

        // GET: /Admin/ProductAttribute
        public async Task<IActionResult> Index()
        {
            // Sắp xếp theo tên để dễ theo dõi
            var attributes = await _db.Attributes.OrderBy(a => a.Name).ToListAsync();
            return View(attributes);
        }

        #endregion

        #region CREATE ACTIONS

        // GET: /Admin/ProductAttribute/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/ProductAttribute/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductAttribute attribute)
        {
            // Logic validation: Kiểm tra xem tên thuộc tính đã tồn tại chưa
            bool isDuplicate = await _db.Attributes.AnyAsync(a => a.Name.ToLower() == attribute.Name.ToLower());
            if (isDuplicate)
            {
                // Thêm lỗi vào ModelState để hiển thị cho người dùng
                ModelState.AddModelError("Name", "Tên thuộc tính này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _db.Attributes.Add(attribute);
                await _db.SaveChangesAsync();
                TempData["success"] = "Tạo thuộc tính thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(attribute);
        }

        #endregion

        #region EDIT ACTIONS

        // GET: /Admin/ProductAttribute/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var attribute = await _db.Attributes.FindAsync(id);
            if (attribute == null) return NotFound();

            return View(attribute);
        }

        // POST: /Admin/ProductAttribute/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductAttribute attribute)
        {
            // Logic validation: Kiểm tra trùng lặp (trừ chính nó)
            bool isDuplicate = await _db.Attributes.AnyAsync(a =>
                a.Name.ToLower() == attribute.Name.ToLower() &&
                a.Id != attribute.Id); // Quan trọng: Loại trừ chính nó ra khỏi kiểm tra

            if (isDuplicate)
            {
                ModelState.AddModelError("Name", "Tên thuộc tính này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _db.Attributes.Update(attribute);
                await _db.SaveChangesAsync();
                TempData["success"] = "Cập nhật thuộc tính thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(attribute);
        }

        #endregion

        #region DELETE ACTIONS

        // GET: /Admin/ProductAttribute/Delete/{id}
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var attribute = await _db.Attributes.FindAsync(id);
            if (attribute == null) return NotFound();

            return View(attribute);
        }

        // POST: /Admin/ProductAttribute/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            var attribute = await _db.Attributes.FindAsync(id);
            if (attribute == null) return NotFound();

            // Logic validation: Kiểm tra xem thuộc tính này có giá trị con nào không
            bool hasChildren = await _db.AttributeValues.AnyAsync(av => av.AttributeId == id);
            if (hasChildren)
            {
                TempData["error"] = "Không thể xóa thuộc tính này vì nó vẫn còn các giá trị con!";
                // Trả về trang Index thay vì trang xác nhận xóa
                return RedirectToAction(nameof(Index));
            }

            _db.Attributes.Remove(attribute);
            await _db.SaveChangesAsync();
            TempData["success"] = "Xóa thuộc tính thành công!";
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}