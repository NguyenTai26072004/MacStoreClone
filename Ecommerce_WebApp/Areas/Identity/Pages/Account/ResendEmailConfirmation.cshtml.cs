// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
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
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        // =================================================================
        // ===== BẮT ĐẦU VÙNG VIỆT HÓA VALIDATION =====
        // =================================================================
        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
            [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
            [Display(Name = "Địa chỉ Email")]
            public string Email { get; set; }
        }
        // =================================================================

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Logic mặc định của Identity là luôn hiển thị thông báo thành công
            // để tránh kẻ xấu dùng form này để dò xem email nào đã tồn tại.
            // Chúng ta chỉ cần Việt hóa thông báo này.
            if (user != null)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = userId, code = code },
                    protocol: Request.Scheme);

                // ===== VIỆT HÓA NỘI DUNG EMAIL =====
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Xác nhận email của bạn",
                    $"Vui lòng xác nhận tài khoản của bạn bằng cách <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>nhấp vào đây</a>.");
            }

            // ===== VIỆT HÓA THÔNG BÁO HIỂN THỊ TRÊN GIAO DIỆN =====
            ModelState.AddModelError(string.Empty, "Gửi email thành công. Vui lòng kiểm tra hộp thư của bạn.");
            return Page();
        }
    }
}