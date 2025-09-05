using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models; 
using Ecommerce_WebApp.ViewModels; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;

        }

        public async Task<IActionResult> Index()
        {
            // Lấy tất cả người dùng
            var users = await _db.Users.ToListAsync();
            var userVMs = new List<UserVM>();

            // Lặp qua từng người dùng để lấy vai trò của họ
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);               
                userVMs.Add(new UserVM
                {                    
                    Id = user.Id,
                    FullName = user.FullName ?? user.UserName, 
                    Email = user.Email,
                    Roles = string.Join(", ", roles),
                    IsLocked = await _userManager.IsLockedOutAsync(user)
                });
            }

            return View(userVMs);
        }

        // === ACTION MỚI - GET: Hiển thị trang quản lý vai trò ===
        public async Task<IActionResult> RoleManagement(string userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Lấy danh sách vai trò hiện tại của người dùng
            var userRoles = await _userManager.GetRolesAsync(user);

            var viewModel = new RoleManagementVM
            {
                User = user,
                // Tạo một danh sách SelectListItem từ tất cả các vai trò trong DB.
                // Đánh dấu 'Selected = true' cho những vai trò mà người dùng đang có.
                RoleList = _roleManager.Roles.Select(role => new SelectListItem
                {
                    Text = role.Name,
                    Value = role.Name,
                    Selected = userRoles.Contains(role.Name)
                })
            };

            return View(viewModel);
        }

        // === ACTION MỚI - POST: Cập nhật vai trò ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleManagement(RoleManagementVM viewModel, List<string> selectedRoles)
        {
            var user = await _db.Users.FindAsync(viewModel.User.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Lấy danh sách vai trò hiện tại
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Xóa tất cả các vai trò cũ
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Thêm các vai trò mới đã được chọn từ form
            if (selectedRoles != null && selectedRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, selectedRoles);
            }

            TempData["success"] = $"Cập nhật vai trò cho người dùng {user.Email} thành công!";
            return RedirectToAction("Index");
        }


        // Action này chuyên dùng để xem đơn hàng của một người dùng cụ thể
        public async Task<IActionResult> UserOrders(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            // Lấy thông tin người dùng để hiển thị làm tiêu đề
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Lấy tất cả các đơn hàng của người dùng này
            var userOrders = await _db.OrderHeaders
                                       .Where(oh => oh.ApplicationUserId == userId)
                                       .OrderByDescending(oh => oh.OrderDate)
                                       .ToListAsync();

            // Gửi tên người dùng sang View để hiển thị tiêu đề
            ViewData["UserIdentifier"] = user.FullName ?? user.Email;

            return View(userOrders); // Trả về một View mới tên là UserOrders
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra xem tài khoản hiện có đang bị khóa hay không
            if (await _userManager.IsLockedOutAsync(user))
            {
                // Nếu đang bị khóa -> Mở khóa
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["success"] = $"Đã mở khóa tài khoản của {user.Email}.";
            }
            else
            {
                // Nếu không bị khóa -> Khóa vĩnh viễn (đặt ngày hết hạn rất xa)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["success"] = $"Đã khóa tài khoản của {user.Email}.";
            }

            return RedirectToAction("Index");
        }
    }
}
