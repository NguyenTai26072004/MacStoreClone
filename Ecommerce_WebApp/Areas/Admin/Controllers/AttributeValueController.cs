using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks; 

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AttributeValueController : Controller
    {
        private readonly AppDbContext _db;

        public AttributeValueController(AppDbContext db)
        {
            _db = db;
        }

        #region READ ACTIONS (Các hành động đọc dữ liệu)

        // GET: /Admin/AttributeValue
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách, sắp xếp theo tên thuộc tính cha rồi đến giá trị
            var attributeValues = await _db.AttributeValues
                .Include(av => av.Attribute)
                .OrderBy(av => av.Attribute.Name)
                .ThenBy(av => av.Value)
                .ToListAsync();

            return View(attributeValues);
        }

        #endregion

        #region CREATE ACTIONS (Các hành động tạo mới)

        // GET: /Admin/AttributeValue/Create
        public async Task<IActionResult> Create()
        {
            // Chuẩn bị dữ liệu cho dropdown và trả về View
            await PrepareAttributeDropdownAsync();
            return View();
        }

        // POST: /Admin/AttributeValue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttributeValue attributeValue)
        {
            if (ModelState.IsValid)
            {
                _db.AttributeValues.Add(attributeValue);
                await _db.SaveChangesAsync();
                TempData["success"] = "Tạo giá trị thuộc tính thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, chuẩn bị lại dropdown và hiển thị lại form
            await PrepareAttributeDropdownAsync();
            return View(attributeValue);
        }

        #endregion

        #region EDIT ACTIONS (Các hành động chỉnh sửa)

        // GET: /Admin/AttributeValue/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var attributeValue = await _db.AttributeValues.FindAsync(id);
            if (attributeValue == null) return NotFound();

            await PrepareAttributeDropdownAsync();
            return View(attributeValue);
        }

        // POST: /Admin/AttributeValue/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AttributeValue attributeValue)
        {
            if (ModelState.IsValid)
            {
                _db.AttributeValues.Update(attributeValue);
                await _db.SaveChangesAsync();
                TempData["success"] = "Cập nhật giá trị thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PrepareAttributeDropdownAsync();
            return View(attributeValue);
        }

        #endregion

        #region DELETE ACTIONS (Các hành động xóa)

        // GET: /Admin/AttributeValue/Delete/{id}
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            // Lấy cả thông tin cha để hiển thị trên trang xác nhận
            var attributeValue = await _db.AttributeValues
                .Include(av => av.Attribute)
                .FirstOrDefaultAsync(av => av.Id == id);

            if (attributeValue == null) return NotFound();
            return View(attributeValue);
        }

        // POST: /Admin/AttributeValue/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            var attributeValue = await _db.AttributeValues.FindAsync(id);
            if (attributeValue == null) return NotFound();

            _db.AttributeValues.Remove(attributeValue);
            await _db.SaveChangesAsync();
            TempData["success"] = "Xóa giá trị thành công!";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region PRIVATE HELPER METHODS (Các hàm hỗ trợ)

        /// <summary>
        /// Chuẩn bị danh sách thuộc tính (dạng SelectListItem) và gán vào ViewBag.
        /// </summary>
        private async Task PrepareAttributeDropdownAsync()
        {
            ViewBag.AttributeList = await _db.Attributes
                .OrderBy(pa => pa.Name)
                .Select(pa => new SelectListItem
                {
                    Text = pa.Name,
                    Value = pa.Id.ToString()
                })
                .ToListAsync();
        }

        #endregion
    }
}