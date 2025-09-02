using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(AppDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        #region Public Actions (Create, Read, Update, Delete)

        // GET: /Admin/Product
        public IActionResult Index()
        {
            var products = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .OrderByDescending(p => p.Id)
                .ToList();
            return View(products);
        }

        // GET: /Admin/Product/Create
        public IActionResult Create()
        {
            var productVM = new ProductVM();
            PopulateViewModelDropdowns(productVM);
            return View(productVM);
        }

        // POST: /Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductVM productVM)
        {
            // Bước 1: Dọn dẹp dữ liệu đầu vào từ form
            productVM.Product.Specifications = CleanSpecifications(productVM.Product.Specifications);

            // Bước 2: Xử lý file ảnh và gán vào model
            productVM.Product.Images = ProcessNewImageUploads(productVM.Images);

            // Bước 3: Kiểm tra validation và lưu
            if (ModelState.IsValid)
            {
                _db.Products.Add(productVM.Product);
                _db.SaveChanges();
                TempData["success"] = "Tạo sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            // Bước 4: Xử lý nếu có lỗi
            TempData["error"] = "Tạo sản phẩm thất bại. Vui lòng kiểm tra lại thông tin.";
            PopulateViewModelDropdowns(productVM);
            return View(productVM);
        }

        // GET: /Admin/Product/Edit/{id}
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var productFromDb = LoadFullProductData(id.Value);
            if (productFromDb == null) return NotFound();

            var productVM = new ProductVM { Product = productFromDb };
            PopulateViewModelDropdowns(productVM);
            return View(productVM);
        }

        // POST: /Admin/Product/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM productVM, List<int> imagesToDelete)
        {
            // Bỏ qua validation cho các collection vì chúng ta xử lý thủ công
            ModelState.Remove("Images");
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Product."))) ModelState.Remove(key);

            if (ModelState.IsValid)
            {
                var productFromDb = _db.Products
                    .Include(p => p.Images)
                    .Include(p => p.Specifications)
                    .Include(p => p.Variants)
                    .FirstOrDefault(p => p.Id == productVM.Product.Id);

                if (productFromDb == null) return NotFound();

                // Gọi các hàm helper để cập nhật từng phần
                UpdateBasicProductInfo(productFromDb, productVM.Product);
                HandleImageUpdates(productFromDb, productVM.Images, imagesToDelete);
                UpdateSpecifications(productFromDb, productVM.Product.Specifications);
                UpdateVariants(productFromDb, productVM.Product.Variants);

                _db.SaveChanges();
                TempData["success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            TempData["error"] = "Cập nhật sản phẩm thất bại. Vui lòng kiểm tra lại thông tin.";
            PopulateViewModelDropdowns(productVM);
            var productInDb = _db.Products.Include(p => p.Images).AsNoTracking().FirstOrDefault(p => p.Id == productVM.Product.Id);
            if (productInDb != null) productVM.Product.Images = productInDb.Images;

            return View(productVM);
        }

        // GET: /Admin/Product/Delete/{id}
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var productFromDb = _db.Products.Include(p => p.Images).FirstOrDefault(p => p.Id == id);
            if (productFromDb == null) return NotFound();
            return View(productFromDb);
        }

        // POST: /Admin/Product/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var productToDelete = _db.Products.Include(p => p.Images).FirstOrDefault(p => p.Id == id);
            if (productToDelete == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm để xóa.";
                return RedirectToAction("Index");
            }

            // Xóa các file ảnh vật lý trước
            if (productToDelete.Images != null)
            {
                foreach (var image in productToDelete.Images.ToList())
                {
                    DeletePhysicalImageFile(image.ImageUrl);
                }
            }

            _db.Products.Remove(productToDelete);
            _db.SaveChanges();
            TempData["success"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        // GET: /Admin/Product/Details/{id}
        public IActionResult Details(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var productFromDb = LoadFullProductData(id.Value);
            if (productFromDb == null) return NotFound();
            return View(productFromDb);
        }

        #endregion

        #region Private Helper Methods (Các hàm xử lý logic nhỏ)

        /// <summary>
        /// Chuẩn bị dữ liệu cho các Dropdown List (Category, Attributes) trong ViewModel.
        /// </summary>
        private void PopulateViewModelDropdowns(ProductVM productVM)
        {
            productVM.CategoryList = _db.Categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
            productVM.AttributeList = _db.Attributes.Include(pa => pa.Values).ToList();
        }

        /// <summary>
        /// Tải đầy đủ thông tin của một sản phẩm, bao gồm tất cả các collection liên quan.
        /// </summary>
        private Product LoadFullProductData(int productId)
        {
            return _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantValues)
                        .ThenInclude(vv => vv.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == productId);
        }

        /// <summary>
        /// Lọc và trả về một danh sách chỉ chứa các thông số kỹ thuật hợp lệ (không rỗng).
        /// </summary>
        private List<ProductSpecification> CleanSpecifications(ICollection<ProductSpecification> specifications)
        {
            if (specifications == null || !specifications.Any())
            {
                return new List<ProductSpecification>();
            }
            return specifications
                .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !string.IsNullOrWhiteSpace(s.Value))
                .ToList();
        }

        /// <summary>
        /// Xử lý các file ảnh mới được tải lên, lưu vào server và trả về danh sách đối tượng ProductImage.
        /// </summary>
        private List<ProductImage> ProcessNewImageUploads(List<IFormFile> imageFiles)
        {
            var newImages = new List<ProductImage>();
            if (imageFiles == null || !imageFiles.Any()) return newImages;

            string productPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

            foreach (var imageFile in imageFiles)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(productPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }
                newImages.Add(new ProductImage { ImageUrl = "/images/products/" + fileName });
            }
            // Tự động gán ảnh đầu tiên trong danh sách tải lên làm ảnh đại diện
            if (newImages.Any()) newImages.First().IsPrimary = true;

            return newImages;
        }

        /// <summary>
        /// Xóa một file ảnh vật lý khỏi thư mục trên server.
        /// </summary>
        private void DeletePhysicalImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('\\', '/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        /// <summary>
        /// Cập nhật các thuộc tính cơ bản của sản phẩm từ ViewModel vào đối tượng từ Database.
        /// </summary>
        private void UpdateBasicProductInfo(Product productFromDb, Product productFromVm)
        {
            productFromDb.Name = productFromVm.Name;
            productFromDb.Description = productFromVm.Description;
            productFromDb.CategoryId = productFromVm.CategoryId;
            productFromDb.IsPublished = productFromVm.IsPublished;
        }

        /// <summary>
        /// Quản lý việc thêm ảnh mới và xóa ảnh được chọn cho một sản phẩm.
        /// </summary>
        private void HandleImageUpdates(Product productFromDb, List<IFormFile> newImageFiles, List<int> imageIdsToDelete)
        {
            // Xử lý xóa ảnh
            if (imageIdsToDelete != null && imageIdsToDelete.Any())
            {
                foreach (var imageId in imageIdsToDelete)
                {
                    var imageToRemove = productFromDb.Images.FirstOrDefault(i => i.Id == imageId);
                    if (imageToRemove != null)
                    {
                        DeletePhysicalImageFile(imageToRemove.ImageUrl);
                        _db.ProductImages.Remove(imageToRemove); // Đánh dấu để EF Core xóa khỏi DB
                    }
                }
            }
            // Xử lý thêm ảnh mới
            var newImages = ProcessNewImageUploads(newImageFiles);
            foreach (var image in newImages)
            {
                productFromDb.Images.Add(image);
            }
        }

        /// <summary>
        /// Cập nhật danh sách thông số kỹ thuật bằng cách xóa hết các thông số cũ và thêm lại các thông số mới hợp lệ.
        /// </summary>
        private void UpdateSpecifications(Product productFromDb, ICollection<ProductSpecification> specsFromVm)
        {
            productFromDb.Specifications.Clear();
            var validSpecifications = CleanSpecifications(specsFromVm);
            foreach (var spec in validSpecifications)
            {
                productFromDb.Specifications.Add(new ProductSpecification { Key = spec.Key, Value = spec.Value, DisplayOrder = spec.DisplayOrder });
            }
        }

        /// <summary>
        /// Cập nhật các thuộc tính của các phiên bản (biến thể) sản phẩm đã tồn tại.
        /// </summary>
        private void UpdateVariants(Product productFromDb, ICollection<ProductVariant> variantsFromVm)
        {
            if (variantsFromVm == null) return;
            foreach (var variantViewModel in variantsFromVm)
            {
                // Chỉ cập nhật các variant đã tồn tại trong DB
                var variantFromDb = productFromDb.Variants.FirstOrDefault(v => v.Id == variantViewModel.Id);
                if (variantFromDb != null)
                {
                    variantFromDb.Price = variantViewModel.Price;
                    variantFromDb.Sku = variantViewModel.Sku;
                    variantFromDb.StockQuantity = variantViewModel.StockQuantity;
                }
            }
        }

        #endregion
    }
}