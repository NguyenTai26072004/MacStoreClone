using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

        // GET: Hiển thị danh sách các giá trị thuộc tính
        public IActionResult Index()
        {

            var attributeValues = _db.AttributeValues.Include(av => av.Attribute).ToList();
            return View(attributeValues);
        }

        // GET: Hiển thị form tạo mới
        public IActionResult Create()
        {
            // Chuẩn bị dữ liệu cho dropdown chọn thuộc tính cha.
            PrepareAttributeDropdown();
            return View();
        }

        // POST: Xử lý tạo mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AttributeValue attributeValue)
        {
            if (ModelState.IsValid)
            {
                _db.AttributeValues.Add(attributeValue);
                _db.SaveChanges();
                TempData["success"] = "Tạo giá trị thuộc tính thành công!";
                return RedirectToAction("Index");
            }

            // Nếu lỗi, chuẩn bị lại dropdown và hiển thị lại form.
            PrepareAttributeDropdown();
            return View(attributeValue);
        }

        // GET: Hiển thị form sửa
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var attributeValue = _db.AttributeValues.Find(id);
            if (attributeValue == null) return NotFound();

            PrepareAttributeDropdown();
            return View(attributeValue);
        }

        // POST: Xử lý sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AttributeValue attributeValue)
        {
            if (ModelState.IsValid)
            {
                _db.AttributeValues.Update(attributeValue);
                _db.SaveChanges();
                TempData["success"] = "Cập nhật giá trị thành công!";
                return RedirectToAction("Index");
            }
            PrepareAttributeDropdown();
            return View(attributeValue);
        }

        // GET: Hiển thị trang xác nhận xóa
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            // Lấy cả thông tin cha để hiển thị trên trang xác nhận.
            var attributeValue = _db.AttributeValues.Include(av => av.Attribute).FirstOrDefault(av => av.Id == id);
            if (attributeValue == null) return NotFound();
            return View(attributeValue);
        }

        // POST: Xử lý xóa
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var attributeValue = _db.AttributeValues.Find(id);
            if (attributeValue == null) return NotFound();
            _db.AttributeValues.Remove(attributeValue);
            _db.SaveChanges();
            TempData["success"] = "Xóa giá trị thành công!";
            return RedirectToAction("Index");
        }

        // --- Phương thức hỗ trợ (Helper Method) ---
        private void PrepareAttributeDropdown()
        {
            // Lấy tất cả các thuộc tính (Màu sắc, RAM...) để làm danh sách chọn.
            ViewBag.AttributeList = _db.Attributes.Select(pa => new SelectListItem
            {
                Text = pa.Name,
                Value = pa.Id.ToString()
            }).ToList();
        }
    }
}