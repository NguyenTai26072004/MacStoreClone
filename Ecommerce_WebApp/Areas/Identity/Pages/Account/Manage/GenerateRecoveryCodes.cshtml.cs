// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Ecommerce_WebApp.Areas.Identity.Pages.Account.Manage
{
    public class GenerateRecoveryCodesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GenerateRecoveryCodesModel> _logger;

        public GenerateRecoveryCodesModel(
            UserManager<ApplicationUser> userManager,
            ILogger<GenerateRecoveryCodesModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     Dùng TempData để lưu trữ các mã khôi phục tạm thời và truyền chúng
        ///     đến trang ShowRecoveryCodes sau khi chuyển hướng.
        /// </summary>
        [TempData]
        public string[] RecoveryCodes { get; set; }

        /// <summary>
        ///     Thông báo thành công sẽ được lưu ở đây và hiển thị trên trang ShowRecoveryCodes.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

            // Kiểm tra xem người dùng đã bật 2FA chưa. Nếu chưa, không thể tạo mã khôi phục.
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isTwoFactorEnabled)
            {
                throw new InvalidOperationException($"Không thể tạo mã khôi phục cho người dùng vì họ chưa bật xác thực hai yếu tố (2FA).");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với ID '{_userManager.GetUserId(User)}'.");
            }

            // Kiểm tra lại lần nữa trước khi thực hiện hành động.
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!isTwoFactorEnabled)
            {
                throw new InvalidOperationException($"Không thể tạo mã khôi phục cho người dùng vì họ chưa bật xác thực hai yếu tố (2FA).");
            }

            // --- HÀNH ĐỘNG CHÍNH ---
            // Tạo 10 mã khôi phục mới. Hành động này cũng sẽ vô hiệu hóa tất cả các mã cũ.
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes = recoveryCodes.ToArray(); // Lưu mã vào TempData

            _logger.LogInformation("Người dùng có ID '{UserId}' đã tạo các mã khôi phục 2FA mới.", userId);

            // Gán thông báo thành công bằng tiếng Việt.
            StatusMessage = "Bạn đã tạo thành công các mã khôi phục mới.";

            // Chuyển hướng người dùng đến trang hiển thị các mã này.
            return RedirectToPage("./ShowRecoveryCodes");
        }
    }
}