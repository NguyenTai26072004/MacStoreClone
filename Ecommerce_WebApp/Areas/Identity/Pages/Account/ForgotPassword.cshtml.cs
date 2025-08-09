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
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        // Constructor này sẽ nhận các dịch vụ đã được đăng ký
        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Tìm người dùng bằng email
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // Logic mặc định: Không gửi email nếu không tìm thấy người dùng hoặc email chưa được xác thực
                // Điều này là để tránh kẻ xấu dùng form của bạn để dò xem email nào đã tồn tại trong hệ thống.
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Không tiết lộ rằng người dùng không tồn tại hoặc chưa xác thực email
                    // Chỉ đơn giản là chuyển hướng như thể đã gửi thành công.
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Tạo mã token để reset mật khẩu
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                // Tạo URL callback để người dùng nhấn vào trong email
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // ===== ĐÂY LÀ DÒNG GỌI ĐẾN IEmailSender =====
                // Breakpoint của bạn nên được kích hoạt khi code chạy đến đây
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Đặt lại mật khẩu của bạn",
                    $"Vui lòng đặt lại mật khẩu của bạn bằng cách <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>nhấp vào đây</a>.");

                // Chuyển hướng đến trang xác nhận đã gửi
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}