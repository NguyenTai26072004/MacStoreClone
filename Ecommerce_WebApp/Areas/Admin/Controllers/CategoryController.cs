using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _db; // Thay bằng tên DbContext của bạn

        public CategoryController(AppDbContext db)
        {
            _db = db;
        }

        // HIỂN THỊ DANH SÁCH DANH MỤC
        // GET: /Admin/Category/Index
        public IActionResult Index()
        {
            // Lấy tất cả danh mục, đồng thời lấy cả thông tin của danh mục cha đi kèm
            // để hiển thị tên của cha trong bảng danh sách.
            List<Category> allCategories = _db.Categories.Include(c => c.Parent).ToList();
            return View(allCategories);
        }

        // HIỂN THỊ FORM TẠO MỚI
        // GET: /Admin/Category/Create
        public IActionResult Create()
        {
            // Chuẩn bị danh sách các danh mục hiện có để đưa vào dropdown "Danh mục cha".
            ViewBag.CategoryList = _db.Categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();

            return View();
        }

        // XỬ LÝ KHI SUBMIT FORM TẠO MỚI
        // POST: /Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category categoryObject)
        {
            // Nếu người dùng không nhập thứ tự hiển thị, tự động tính toán.
            if (!categoryObject.DisplayOrder.HasValue)
            {
                // Lọc các danh mục cùng cấp (cùng cha).
                var siblings = _db.Categories.Where(c => c.ParentId == categoryObject.ParentId);
                int maxOrder = siblings.Any() ? siblings.Max(c => c.DisplayOrder) ?? 0 : 0;
                categoryObject.DisplayOrder = maxOrder + 1;
            }

            // Kiểm tra xem thứ tự hiển thị đã tồn tại trong cùng cấp cha chưa.
            bool displayOrderExists = _db.Categories.Any(c =>
                c.DisplayOrder == categoryObject.DisplayOrder &&
                c.ParentId == categoryObject.ParentId);

            if (displayOrderExists)
            {
                // Nếu trùng, thêm lỗi vào ModelState để hiển thị cho người dùng.
                ModelState.AddModelError("DisplayOrder", "Thứ tự hiển thị này đã tồn tại trong cùng danh mục cha.");
            }

            // Nếu tất cả dữ liệu đều hợp lệ (bao gồm cả lỗi tự thêm ở trên).
            if (ModelState.IsValid)
            {
                _db.Categories.Add(categoryObject);
                _db.SaveChanges();
                TempData["success"] = "Tạo danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Nếu có lỗi, phải chuẩn bị lại danh sách dropdown trước khi trả về View.
            ViewBag.CategoryList = _db.Categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();

            return View(categoryObject);
        }

        // HIỂN THỊ FORM CHỈNH SỬA
        // GET: /Admin/Category/Edit/{id}
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            // Chuẩn bị danh sách dropdown, loại trừ chính danh mục này ra để không tự làm cha của chính mình.
            ViewBag.CategoryList = _db.Categories.Where(c => c.Id != id).Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();

            return View(categoryFromDb);
        }

        // XỬ LÝ KHI SUBMIT FORM CHỈNH SỬA
        // POST: /Admin/Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category categoryObject)
        {
            // Kiểm tra trùng lặp thứ tự hiển thị, nhưng phải loại trừ chính nó ra.
            bool displayOrderExists = _db.Categories.Any(c =>
                c.DisplayOrder == categoryObject.DisplayOrder &&
                c.ParentId == categoryObject.ParentId &&
                c.Id != categoryObject.Id);

            if (displayOrderExists)
            {
                ModelState.AddModelError("DisplayOrder", "Thứ tự hiển thị này đã tồn tại trong cùng danh mục cha.");
            }

            if (ModelState.IsValid)
            {
                _db.Categories.Update(categoryObject);
                _db.SaveChanges();
                TempData["success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Nếu có lỗi, chuẩn bị lại dropdown và trả về View.
            ViewBag.CategoryList = _db.Categories.Where(c => c.Id != categoryObject.Id).Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();

            return View(categoryObject);
        }

        // HIỂN THỊ TRANG XÁC NHẬN XÓA
        // GET: /Admin/Category/Delete/{id}
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        // XỬ LÝ KHI XÁC NHẬN XÓA
        // POST: /Admin/Category/DeletePOST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var objToDelete = _db.Categories.Find(id);
            if (objToDelete == null)
            {
                return NotFound();
            }

            // Quy tắc an toàn: Không cho xóa nếu danh mục này vẫn còn con.
            bool hasChildren = _db.Categories.Any(c => c.ParentId == id);
            if (hasChildren)
            {
                TempData["error"] = "Không thể xóa danh mục này vì nó vẫn còn danh mục con!";
                return RedirectToAction("Index");
            }

            _db.Categories.Remove(objToDelete);
            _db.SaveChanges();
            TempData["success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }
    }
}