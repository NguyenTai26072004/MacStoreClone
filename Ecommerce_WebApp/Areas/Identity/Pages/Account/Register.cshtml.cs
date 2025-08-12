// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce_WebApp.Services; 
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Ecommerce_WebApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        //----- CÁC DỊCH VỤ ĐƯỢC TIÊM VÀO (DEPENDENCY INJECTION) -----

        // Quản lý người dùng: tạo, xóa, tìm kiếm, gán mật khẩu, gán vai trò.
        private readonly UserManager<ApplicationUser> _userManager;
        // Quản lý việc đăng nhập/đăng xuất của người dùng.
        private readonly SignInManager<ApplicationUser> _signInManager;
        // Dịch vụ gửi email (được đăng ký trong Program.cs).
        private readonly IEmailSender _emailSender;
        // Các dịch vụ hỗ trợ khác của Identity.
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        // Constructor: Nơi các dịch vụ được "tiêm" vào khi một instance của RegisterModel được tạo ra.
        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        //----- CÁC THUỘC TÍNH (PROPERTIES) -----

        /// <summary>
        ///     Model này chứa dữ liệu được gửi từ form đăng ký (Email, Mật khẩu...).
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     URL để chuyển hướng người dùng sau khi đăng ký thành công.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     Danh sách các nhà cung cấp đăng nhập bên ngoài (Google, Facebook...).
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     Lớp con định nghĩa các trường dữ liệu trên form đăng ký.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
            [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
            [Display(Name = "Địa chỉ Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [StringLength(100, ErrorMessage = "{0} phải có độ dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không trùng khớp.")]
            public string ConfirmPassword { get; set; }
        }

        //----- CÁC PHƯƠNG THỨC XỬ LÝ (PAGE HANDLERS) -----

        /// <summary>
        ///     Phương thức được gọi khi trang được tải bằng phương thức GET (vào trang đăng ký).
        /// </summary>
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        ///     Phương thức được gọi khi người dùng nhấn nút "Đăng ký" (submit form bằng phương thức POST).
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            //----- BƯỚC 1: KIỂM TRA TÍNH HỢP LỆ CỦA DỮ LIỆU GỬI LÊN -----
            if (ModelState.IsValid)
            {
                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Thực hiện tạo người dùng trong database
                var result = await _userManager.CreateAsync(user, Input.Password);

                //----- BƯỚC 2: XỬ LÝ KHI TẠO USER THÀNH CÔNG -----
                if (result.Succeeded)
                {
                    _logger.LogInformation("Người dùng đã tạo một tài khoản mới bằng mật khẩu.");

                    // =============================================================================
                    //    GÁN VAI TRÒ (ROLE) MẶC ĐỊNH CHO NGƯỜI DÙNG MỚI
                    //    Đây là phần quan trọng để phân quyền sau này.
                    //    Mọi tài khoản đăng ký qua form này sẽ được gán vai trò "Customer".
                    // =============================================================================
                    await _userManager.AddToRoleAsync(user, "Customer");

                    //----- BƯỚC 3: CHUẨN BỊ VÀ GỬI EMAIL XÁC NHẬN -----

                    // Tạo mã token để xác nhận email
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    // Tạo đường link xác nhận hoàn chỉnh
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    // Chuẩn bị các giá trị để điền vào email template
                    var replacements = new Dictionary<string, string>
                    {
                        { "{{UserName}}", Input.Email },
                        { "{{CallbackUrl}}", callbackUrl }
                    };

                    // Gửi email bằng phương thức đã được custom sử dụng template
                    if (_emailSender is EmailSender sender)
                    {
                        await sender.SendEmailFromTemplateAsync(
                            Input.Email,
                            "Xác nhận tài khoản của bạn",
                            "ConfirmAccountTemplate.html", // Tên file template trong wwwroot/EmailTemplates
                            replacements
                        );
                    }
                    else
                    {
                        // Phương thức dự phòng nếu ép kiểu không thành công
                        await _emailSender.SendEmailAsync(Input.Email, "Xác nhận email của bạn",
                            $"Vui lòng xác nhận tài khoản của bạn bằng cách <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>nhấp vào đây</a>.");
                    }

                    //----- BƯỚC 4: ĐIỀU HƯỚNG NGƯỜI DÙNG -----
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        // Nếu bắt buộc xác nhận email, chuyển đến trang thông báo
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // Nếu không, đăng nhập người dùng luôn và chuyển về trang trước đó
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                //----- BƯỚC 5: XỬ LÝ KHI TẠO USER THẤT BẠI -----
                // Thêm các lỗi từ Identity (vd: mật khẩu quá yếu, email đã tồn tại) vào ModelState để hiển thị cho người dùng
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Nếu ModelState không hợp lệ, trả về lại trang đăng ký để người dùng sửa lỗi.
            return Page();
        }

        //----- CÁC PHƯƠNG THỨC HỖ TRỢ (PRIVATE HELPERS) -----
        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Không thể tạo một thực thể của '{nameof(ApplicationUser)}'. " +
                    $"Hãy đảm bảo '{nameof(ApplicationUser)}' không phải là một lớp trừu tượng và có một hàm khởi tạo không tham số.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("Giao diện người dùng mặc định yêu cầu một user store có hỗ trợ email.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}