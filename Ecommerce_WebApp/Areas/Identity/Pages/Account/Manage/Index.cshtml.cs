#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ecommerce_WebApp.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }


        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Họ và Tên")]
            public string FullName { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Ngày sinh")]
            public DateTime? DateOfBirth { get; set; }
        }


        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Lấy thông tin người dùng hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

            // Kiểm tra xem dữ liệu từ form có hợp lệ không
            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // --- BẮT ĐẦU CẬP NHẬT THÔNG TIN ---

            bool hasChanges = false;

            // 1. Cập nhật Họ và Tên nếu có thay đổi
            if (Input.FullName != user.FullName)
            {
                user.FullName = Input.FullName;
                hasChanges = true;
            }

            // 2. Cập nhật Ngày sinh nếu có thay đổi
            if (Input.DateOfBirth != user.DateOfBirth)
            {
                user.DateOfBirth = Input.DateOfBirth;
                hasChanges = true;
            }

            // 3. Cập nhật Số điện thoại nếu có thay đổi
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Lỗi không mong muốn khi cố gắng cập nhật số điện thoại.";
                    return RedirectToPage();
                }
                // SetPhoneNumberAsync đã tự động lưu, nên ta coi như có thay đổi
                hasChanges = true;
            }

            // 4. Lưu tất cả thay đổi vào database nếu có
            // (Chỉ gọi UpdateAsync nếu SetPhoneNumberAsync chưa được gọi và có thay đổi khác)
            if (hasChanges && Input.PhoneNumber == phoneNumber)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Lỗi không mong muốn khi cố gắng cập nhật hồ sơ.";
                    return RedirectToPage();
                }
            }

            // --- KẾT THÚC CẬP NHẬT ---
            // Làm mới cookie đăng nhập để áp dụng thay đổi ngay lập tức
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Hồ sơ của bạn đã được cập nhật thành công.";
            return RedirectToPage();
        }
    }
}