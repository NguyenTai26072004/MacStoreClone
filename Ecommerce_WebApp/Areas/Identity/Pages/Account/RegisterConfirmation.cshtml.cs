// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Ecommerce_WebApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        // IEmailSender không còn cần thiết ở trang này, vì email đã được gửi từ trang Register

        public RegisterConfirmationModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Địa chỉ email của người dùng, dùng để hiển thị trên giao diện.
        /// </summary>
        public string Email { get; set; }

        // Các thuộc tính DisplayConfirmAccountLink và EmailConfirmationUrl đã được loại bỏ
        // vì chúng ta đã có một dịch vụ gửi email thực sự.

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                // Nếu không có email, chuyển hướng về trang chủ
                return RedirectToPage("/Home");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Không thể tải người dùng với email '{email}'.");
            }

            // Gán email để hiển thị trên View
            Email = email;

            return Page();
        }
    }
}