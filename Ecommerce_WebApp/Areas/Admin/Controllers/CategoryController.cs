using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.ViewModels; 
using Microsoft.AspNetCore.Hosting; 
using Microsoft.EntityFrameworkCore;
using System.IO; 
using System.Linq;

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(AppDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Hiển thị danh sách
        public IActionResult Index()
        {
            List<Category> allCategories = _db.Categories.Include(c => c.Parent).OrderBy(c => c.DisplayOrder).ToList();
            return View(allCategories);
        }

        // GET: Hiển thị form tạo mới
        public IActionResult Create()
        {
            var viewModel = new CategoryFormVM
            {
                Category = new Category(),
                ParentCategoryList = _db.Categories.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
            };
            return View(viewModel);
        }

        // POST: Xử lý tạo mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryFormVM viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Lấy ra tất cả các cặp Key-Value trong ModelState
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                // Đoạn code trên sẽ gom tất cả lỗi lại.
                // Bạn chỉ cần đặt breakpoint ở dòng dưới đây để xem.
                // Khi chương trình dừng lại, hãy di chuột lên biến "errors"
                // và xem nội dung của nó.
            }

            if (ModelState.IsValid)
            {
                // Xử lý upload file icon
                if (viewModel.IconImage != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.IconImage.FileName);
                    string categoryPath = Path.Combine(wwwRootPath, @"img\categories");

                    if (!Directory.Exists(categoryPath)) Directory.CreateDirectory(categoryPath);

                    using (var fileStream = new FileStream(Path.Combine(categoryPath, fileName), FileMode.Create))
                    {
                        viewModel.IconImage.CopyTo(fileStream);
                    }
                    viewModel.Category.IconUrl = @"/img/categories/" + fileName;
                }

                _db.Categories.Add(viewModel.Category);
                _db.SaveChanges();
                TempData["success"] = "Tạo danh mục thành công!";
                return RedirectToAction("Index");
            }

            viewModel.ParentCategoryList = _db.Categories.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            return View(viewModel);
        }

        // GET: Hiển thị form sửa
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null) return NotFound();

            var viewModel = new CategoryFormVM
            {
                Category = categoryFromDb,
                ParentCategoryList = _db.Categories.Where(c => c.Id != id).Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
            };
            return View(viewModel);
        }

        // POST: Xử lý sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CategoryFormVM viewModel)
        {
            // Bỏ qua validation cho file ảnh mới, vì người dùng có thể không muốn thay đổi ảnh
            ModelState.Remove("IconImage");

            if (ModelState.IsValid)
            {
                var categoryFromDb = _db.Categories.Find(viewModel.Category.Id);
                if (categoryFromDb == null) return NotFound();

                // Cập nhật thông tin
                categoryFromDb.Name = viewModel.Category.Name;
                categoryFromDb.ParentId = viewModel.Category.ParentId;
                categoryFromDb.DisplayOrder = viewModel.Category.DisplayOrder;

                // Xử lý upload ảnh mới (nếu có)
                if (viewModel.IconImage != null)
                {
                    // Xóa ảnh cũ (nếu có)
                    if (!string.IsNullOrEmpty(categoryFromDb.IconUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, categoryFromDb.IconUrl.TrimStart('\\', '/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    // Lưu ảnh mới
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.IconImage.FileName);
                    string categoryPath = Path.Combine(wwwRootPath, @"img\categories");
                    using (var fileStream = new FileStream(Path.Combine(categoryPath, fileName), FileMode.Create))
                    {
                        viewModel.IconImage.CopyTo(fileStream);
                    }
                    categoryFromDb.IconUrl = @"/img/categories/" + fileName;
                }

                _db.Categories.Update(categoryFromDb);
                _db.SaveChanges();
                TempData["success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            viewModel.ParentCategoryList = _db.Categories.Where(c => c.Id != viewModel.Category.Id).Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            return View(viewModel);
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