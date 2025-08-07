// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Ecommerce_WebApp.Areas.Identity.Pages.Account
{
    // Trang này không cần [AllowAnonymous] vì nó không xử lý thông tin nhạy cảm
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ConfirmEmailModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                // Nếu thiếu thông tin, chuyển về trang chủ
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            // ===== VIỆT HÓA THÔNG BÁO TRẠNG THÁI =====
            StatusMessage = result.Succeeded
                ? "Cảm ơn bạn đã xác thực email. Tài khoản của bạn đã được kích hoạt!"
                : "Lỗi: Không thể xác thực email của bạn.";

            return Page();
        }
    }
}