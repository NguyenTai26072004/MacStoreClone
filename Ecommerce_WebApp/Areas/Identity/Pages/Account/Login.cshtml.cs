// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

// Đảm bảo bạn đã có using cho ApplicationUser của bạn nếu nó ở namespace khác
// using Ecommerce_WebApp.Models; 

namespace Ecommerce_WebApp.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager; 
        private readonly ILogger<LoginModel> _logger;

        //CONSTRUCTOR
        public LoginModel(SignInManager<ApplicationUser> signInManager,
                          UserManager<ApplicationUser> userManager, 
                          ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager; 
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }


        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
            [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
            [Display(Name = "Địa chỉ Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ tôi?")]
            public bool RememberMe { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        // CẬP NHẬT PHƯƠNG THỨC OnPostAsync
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // === PHẦN LOGIC CHUYỂN HƯỚNG DỰA TRÊN ROLE ===
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            // Nếu là Admin, chuyển hướng đến trang HomeAdmin
                            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Customer"))
                        {
                            // Nếu là Customer, chuyển hướng đến trang Home
                            return RedirectToAction("Index", "Home");
                        }
                    }
 

                    // Nếu không có vai trò cụ thể hoặc có returnUrl, quay lại trang trước đó
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không chính xác.");
                    return Page();
                }
            }

  
            return Page();
        }
    }
}