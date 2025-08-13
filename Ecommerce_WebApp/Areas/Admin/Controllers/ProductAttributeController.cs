using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;


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

        // GET: Hiển thị danh sách thuộc tính
        public IActionResult Index()
        {
            var attributes = _db.Attributes.ToList();
            return View(attributes);
        }

        // GET: Hiển thị form tạo mới
        public IActionResult Create()
        {
            return View();
        }

        // POST: Xử lý tạo mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductAttribute attribute)
        {
            if (ModelState.IsValid)
            {
                _db.Attributes.Add(attribute);
                _db.SaveChanges();
                TempData["success"] = "Tạo thuộc tính thành công!";
                return RedirectToAction("Index");
            }
            return View(attribute);
        }

        // GET: Hiển thị form sửa
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var attribute = _db.Attributes.Find(id);
            if (attribute == null) return NotFound();
            return View(attribute);
        }

        // POST: Xử lý sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductAttribute attribute)
        {
            if (ModelState.IsValid)
            {
                _db.Attributes.Update(attribute);
                _db.SaveChanges();
                TempData["success"] = "Cập nhật thuộc tính thành công!";
                return RedirectToAction("Index");
            }
            return View(attribute);
        }

        // GET: Hiển thị trang xác nhận xóa
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var attribute = _db.Attributes.Find(id);
            if (attribute == null) return NotFound();
            return View(attribute);
        }

        // POST: Xử lý xóa
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var attribute = _db.Attributes.Find(id);
            if (attribute == null) return NotFound();
            _db.Attributes.Remove(attribute);
            _db.SaveChanges();
            TempData["success"] = "Xóa thuộc tính thành công!";
            return RedirectToAction("Index");
        }
    }
}