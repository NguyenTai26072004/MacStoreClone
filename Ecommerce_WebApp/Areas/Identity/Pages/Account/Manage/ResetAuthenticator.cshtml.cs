// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

// Đảm bảo namespace này khớp với project của bạn
namespace Ecommerce_WebApp.Areas.Identity.Pages.Account.Manage
{
    public class ResetAuthenticatorModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ResetAuthenticatorModel> _logger;

        public ResetAuthenticatorModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ResetAuthenticatorModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        ///     Thuộc tính này dùng để truyền một thông báo tạm thời giữa các request.
        ///     Ví dụ: sau khi đặt lại khóa thành công và chuyển hướng, thông báo này sẽ được hiển thị.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

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

            //----- LOGIC CỐT LÕI -----
            // 1. Tắt tạm thời 2FA để bắt đầu quá trình reset.
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            // 2. Tạo một khóa xác thực mới và lưu vào database. Khóa cũ bị vô hiệu hóa.
            await _userManager.ResetAuthenticatorKeyAsync(user);

            // Ghi log lại hành động này vì nó liên quan đến bảo mật
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("Người dùng có ID '{UserId}' đã đặt lại khóa ứng dụng xác thực của họ.", user.Id);

            // Cập nhật lại cookie đăng nhập để phản ánh thay đổi
            await _signInManager.RefreshSignInAsync(user);

            // ========================= THAY ĐỔI Ở ĐÂY =========================
            // Gán thông báo thành công bằng tiếng Việt để hiển thị ở trang tiếp theo.
            StatusMessage = "Khóa xác thực của bạn đã được đặt lại thành công. Bạn sẽ cần cấu hình lại ứng dụng xác thực của mình bằng khóa mới.";
            // ================================================================

            // Chuyển hướng người dùng đến trang "Kích hoạt ứng dụng xác thực" để họ bắt đầu cấu hình lại với khóa mới.
            return RedirectToPage("./EnableAuthenticator");
        }
    }
}