using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        // GET: /Admin/Product/Index
        public IActionResult Index()
        {
            // Lấy tất cả sản phẩm. Chúng ta sẽ cần .Include() nhiều thứ để hiển thị.
            // - Category: để lấy tên danh mục.
            // - Images: để lấy ảnh đại diện.
            var products = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToList();

            return View(products);
        }

        // GET: /Admin/Product/Create
        public IActionResult Create()
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _db.Categories.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                AttributeList = _db.Attributes.Include(pa => pa.Values).ToList()

            };

            return View(productVM);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductVM productVM)
        {
            // Chúng ta sẽ kiểm tra ModelState ở cuối, sau khi đã xử lý xong file và các logic khác.

            // =======================================================
            // BƯỚC 1: XỬ LÝ UPLOAD HÌNH ẢNH
            // =======================================================
            if (productVM.Images != null && productVM.Images.Count > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string productPath = Path.Combine(wwwRootPath, @"images\products");

                if (!Directory.Exists(productPath))
                {
                    Directory.CreateDirectory(productPath);
                }

                foreach (var imageFile in productVM.Images)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(productPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(fileStream);
                    }

                    var productImage = new ProductImage
                    {
                        ImageUrl = @"/images/products/" + fileName,
                        IsPrimary = productVM.Product.Images.Count == 0
                    };
                    productVM.Product.Images.Add(productImage);
                }
            }

            // =======================================================
            // BƯỚC 2: KIỂM TRA VALIDATION VÀ LƯU VÀO DATABASE
            // =======================================================
            if (ModelState.IsValid)
            {
                // Thêm sản phẩm gốc vào DbContext.
                // Entity Framework sẽ tự động theo dõi đối tượng này và tất cả các đối tượng con của nó
                // (Images, Specifications, và ĐẶC BIỆT LÀ Variants cùng với VariantValues bên trong).
                _db.Products.Add(productVM.Product);

                // Khi gọi SaveChanges(), EF sẽ thực hiện một giao dịch (transaction):
                // 1. INSERT vào bảng Products.
                // 2. Lấy ProductId mới được tạo.
                // 3. INSERT vào ProductImages với ProductId đó.
                // 4. INSERT vào ProductSpecifications với ProductId đó.
                // 5. Lặp qua từng ProductVariant:
                //    a. INSERT vào ProductVariants với ProductId đó.
                //    b. Lấy ProductVariantId mới được tạo.
                //    c. Lặp qua từng VariantValue của nó, INSERT vào VariantValues với ProductVariantId đó.
                _db.SaveChanges();

                TempData["success"] = "Tạo sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            // =======================================================
            // BƯỚC 3: XỬ LÝ NẾU CÓ LỖI VALIDATION
            // =======================================================
            // Nếu có lỗi, phải chuẩn bị lại toàn bộ dữ liệu cho View.
            TempData["error"] = "Tạo sản phẩm thất bại. Vui lòng kiểm tra lại các thông tin đã nhập.";
            productVM.CategoryList = _db.Categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
            productVM.AttributeList = _db.Attributes.Include(pa => pa.Values).ToList();

            return View(productVM);
        }


        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            // Câu truy vấn đã được nâng cấp để lấy tất cả dữ liệu cần thiết cho View
            var productFromDb = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantValues)
                        .ThenInclude(vv => vv.AttributeValue) // Lấy AttributeValue
                            .ThenInclude(av => av.Attribute)    // Từ AttributeValue, lấy tiếp ProductAttribute
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            ProductVM productVM = new ProductVM()
            {
                Product = productFromDb,
                CategoryList = _db.Categories.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                AttributeList = _db.Attributes.Include(pa => pa.Values).ToList()
            };

            return View(productVM);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM productVM, List<int> imagesToDelete)
        {
            // Bỏ qua validation cho các collection, chúng ta sẽ xử lý thủ công
            ModelState.Remove("Images");
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Product.Variants"))) ModelState.Remove(key);
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Product.Specifications"))) ModelState.Remove(key);


            if (ModelState.IsValid)
            {
                // Tải đối tượng gốc từ DB và các collection liên quan để cập nhật
                var productFromDb = _db.Products
                    .Include(p => p.Images)
                    .Include(p => p.Specifications)
                    .Include(p => p.Variants)
                    .FirstOrDefault(p => p.Id == productVM.Product.Id);

                if (productFromDb == null) return NotFound();

                // ===== 1. CẬP NHẬT THÔNG TIN CƠ BẢN =====
                productFromDb.Name = productVM.Product.Name;
                productFromDb.Description = productVM.Product.Description;
                productFromDb.CategoryId = productVM.Product.CategoryId;

                // ===== 2. THÊM ẢNH MỚI =====
                if (productVM.Images != null && productVM.Images.Count > 0)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string productPath = Path.Combine(wwwRootPath, @"img\products");
                    if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                    foreach (var imageFile in productVM.Images)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(productPath, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { imageFile.CopyTo(fileStream); }
                        productFromDb.Images.Add(new ProductImage { ImageUrl = @"/img/products/" + fileName });
                    }
                }

                // ===== 3. XÓA ẢNH CŨ =====
                if (imagesToDelete != null && imagesToDelete.Count > 0)
                {
                    foreach (var imageId in imagesToDelete)
                    {
                        var imageToRemove = productFromDb.Images.FirstOrDefault(i => i.Id == imageId);
                        if (imageToRemove != null)
                        {
                            // Xóa file vật lý trên server
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToRemove.ImageUrl.TrimStart('\\', '/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            // Xóa bản ghi trong database
                            _db.ProductImages.Remove(imageToRemove);
                        }
                    }
                }

                // ===== 4. CẬP NHẬT THÔNG SỐ KỸ THUẬT (Xóa hết, thêm lại) =====
                // Xóa tất cả các thông số cũ
                productFromDb.Specifications.Clear();
                // Thêm lại danh sách mới từ form
                if (productVM.Product.Specifications != null)
                {
                    foreach (var spec in productVM.Product.Specifications)
                    {
                        productFromDb.Specifications.Add(new ProductSpecification { Key = spec.Key, Value = spec.Value, DisplayOrder = spec.DisplayOrder });
                    }
                }

                // ===== 5. CẬP NHẬT CÁC PHIÊN BẢN HIỆN TẠI =====
                if (productVM.Product.Variants != null)
                {
                    foreach (var variantViewModel in productVM.Product.Variants)
                    {
                        var variantFromDb = productFromDb.Variants.FirstOrDefault(v => v.Id == variantViewModel.Id);
                        if (variantFromDb != null)
                        {
                            variantFromDb.Price = variantViewModel.Price;
                            variantFromDb.Sku = variantViewModel.Sku;
                            variantFromDb.StockQuantity = variantViewModel.StockQuantity;
                        }
                    }
                }

                _db.SaveChanges(); // Lưu tất cả các thay đổi vào DB trong một lần gọi

                TempData["success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }


            return View(productVM);
        }



        // GET: /Admin/Product/Delete/{id}
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            // Lấy thông tin sản phẩm và ảnh đại diện để hiển thị
            var productFromDb = _db.Products
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }
            return View(productFromDb);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            // Tìm sản phẩm và các hình ảnh của nó để có thể xóa file
            var productToDelete = _db.Products.Include(p => p.Images).FirstOrDefault(p => p.Id == id);
            if (productToDelete == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm để xóa.";
                return RedirectToAction("Index");
            }

            // === BƯỚC 1: XÓA CÁC FILE ẢNH VẬT LÝ ===
            if (productToDelete.Images != null && productToDelete.Images.Any())
            {
                foreach (var image in productToDelete.Images)
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('\\', '/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
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
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var productFromDb = _db.Products
                .Include(p => p.Category) 
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantValues)
                        .ThenInclude(vv => vv.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

    }
}