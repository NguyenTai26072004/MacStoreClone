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
            var viewModel = new CategoryFormVM();
            PopulateParentCategoryList(viewModel);
            return View(viewModel);
        }

        // POST: Xử lý tạo mới - ĐÃ HOÀN THIỆN VÀ TÁCH HÀM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryFormVM viewModel)
        {
            if (_db.Categories.Any(c => c.Name == viewModel.Category.Name))
            {
                ModelState.AddModelError("Category.Name", "Tên danh mục này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                // Gọi hàm helper để xử lý ảnh
                viewModel.Category.IconUrl = ProcessUploadedIcon(viewModel.IconImage);

                // Gọi hàm helper để xử lý thứ tự hiển thị
                if (!viewModel.Category.DisplayOrder.HasValue)
                {
                    viewModel.Category.DisplayOrder = GetNextDisplayOrder(viewModel.Category.ParentId);
                }

                _db.Categories.Add(viewModel.Category);
                _db.SaveChanges();
                TempData["success"] = "Tạo danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Nếu không hợp lệ, gọi hàm helper để tải lại danh sách và hiển thị lại form
            PopulateParentCategoryList(viewModel);
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
                Category = categoryFromDb
            };

            PopulateParentCategoryList(viewModel, id);
            return View(viewModel);
        }

        // POST: Xử lý sửa - ĐÃ HOÀN THIỆN VÀ TÁCH HÀM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CategoryFormVM viewModel)
        {
            if (_db.Categories.Any(c => c.Name == viewModel.Category.Name && c.Id != viewModel.Category.Id))
            {
                ModelState.AddModelError("Category.Name", "Tên danh mục này đã tồn tại.");
            }

            ModelState.Remove("IconImage");

            if (ModelState.IsValid)
            {
                // Dùng AsNoTracking để lấy thông tin ảnh cũ mà không gây xung đột tracking
                var categoryFromDb = _db.Categories.AsNoTracking().FirstOrDefault(c => c.Id == viewModel.Category.Id);
                if (categoryFromDb == null) return NotFound();

                // Gọi hàm helper để xử lý ảnh (nếu có ảnh mới)
                // Nó sẽ tự động xóa ảnh cũ nếu có ảnh mới được tải lên
                if (viewModel.IconImage != null)
                {
                    viewModel.Category.IconUrl = ProcessUploadedIcon(viewModel.IconImage, categoryFromDb.IconUrl);
                }
                else
                {
                    // Nếu không có ảnh mới, giữ lại đường dẫn ảnh cũ
                    viewModel.Category.IconUrl = categoryFromDb.IconUrl;
                }

                // Nếu người dùng xóa trắng ô DisplayOrder, giữ lại giá trị cũ
                if (!viewModel.Category.DisplayOrder.HasValue)
                {
                    viewModel.Category.DisplayOrder = categoryFromDb.DisplayOrder;
                }

                _db.Categories.Update(viewModel.Category);
                _db.SaveChanges();
                TempData["success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Nếu không hợp lệ, gọi hàm helper để tải lại danh sách và hiển thị lại form
            PopulateParentCategoryList(viewModel, viewModel.Category.Id);
            return View(viewModel);
        }

        // Các hàm Delete giữ nguyên
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null) return NotFound();
            return View(categoryFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var objToDelete = _db.Categories.Find(id);
            if (objToDelete == null) return NotFound();

            if (_db.Categories.Any(c => c.ParentId == id))
            {
                TempData["error"] = "Không thể xóa danh mục này vì nó vẫn còn danh mục con!";
                return RedirectToAction("Index");
            }

            // Xóa ảnh của danh mục trước khi xóa danh mục
            ProcessUploadedIcon(null, objToDelete.IconUrl);

            _db.Categories.Remove(objToDelete);
            _db.SaveChanges();
            TempData["success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }

        #region PRIVATE HELPER METHODS

        /// <summary>
        /// Xử lý file ảnh được tải lên.
        /// </summary>
        /// <param name="iconImage">File ảnh từ form.</param>
        /// <param name="oldImageUrl">Đường dẫn ảnh cũ (nếu có) để xóa.</param>
        /// <returns>Đường dẫn URL của ảnh mới, hoặc giữ lại đường dẫn cũ, hoặc null.</returns>
        private string? ProcessUploadedIcon(IFormFile? iconImage, string? oldImageUrl = null)
        {
            // 1. Xóa ảnh cũ nếu nó tồn tại
            if (!string.IsNullOrEmpty(oldImageUrl))
            {
                // Chỉ xóa ảnh cũ khi có ảnh mới tải lên, hoặc khi xóa hẳn một danh mục
                if (iconImage != null || TempData.ContainsKey("DeleteAction"))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, oldImageUrl.TrimStart('\\', '/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
            }

            // Nếu không có ảnh mới tải lên, trả về đường dẫn ảnh cũ (hoặc null)
            if (iconImage == null)
            {
                // Nếu đang xóa, trả về null. Nếu không, giữ lại ảnh cũ
                return TempData.ContainsKey("DeleteAction") ? null : oldImageUrl;
            }

            // 2. Lưu ảnh mới
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(iconImage.FileName);
            string categoryPath = Path.Combine(wwwRootPath, "img", "categories");

            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            string filePath = Path.Combine(categoryPath, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                iconImage.CopyTo(fileStream);
            }

            // 3. Trả về đường dẫn URL để lưu vào database
            return "/img/categories/" + fileName;
        }

        /// <summary>
        /// Tính toán thứ tự hiển thị tiếp theo DỰA TRÊN CẤP ĐỘ (cha/con).
        /// </summary>
        /// <param name="parentId">ID của danh mục cha. Null nếu là danh mục gốc.</param>
        /// <returns>Số thứ tự hiển thị.</returns>
        private int GetNextDisplayOrder(int? parentId)
        {
            // Lọc ra các danh mục cùng cấp
            var siblingCategories = _db.Categories.Where(c => c.ParentId == parentId);

            // Nếu không có danh mục nào cùng cấp, đây là mục đầu tiên
            if (!siblingCategories.Any())
            {
                return 1;
            }

            // Nếu có, tìm thứ tự lớn nhất trong các mục cùng cấp và cộng thêm 1
            // (?? 0) để xử lý trường hợp tất cả DisplayOrder đều là null
            return (siblingCategories.Max(c => (int?)c.DisplayOrder) ?? 0) + 1;
        }


        /// <summary>
        /// Chuẩn bị danh sách danh mục cha cho dropdown.
        /// </summary>
        /// <param name="viewModel">ViewModel cần được gán danh sách.</param>
        /// <param name="categoryIdToExclude">ID của danh mục hiện tại (dùng cho form Edit) để loại nó ra khỏi danh sách.</param>
        private void PopulateParentCategoryList(CategoryFormVM viewModel, int? categoryIdToExclude = null)
        {
            var query = _db.Categories.AsQueryable();

            if (categoryIdToExclude != null)
            {
                // Loại bỏ chính danh mục đang sửa và các con của nó khỏi danh sách
                query = query.Where(c => c.Id != categoryIdToExclude.Value && c.ParentId != categoryIdToExclude.Value);
            }

            viewModel.ParentCategoryList = query
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
                .ToList();
        }

        #endregion
    }
}