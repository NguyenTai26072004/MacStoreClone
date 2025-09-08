using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Ecommerce_WebApp.Services;
using Ecommerce_WebApp.Utility;
using Microsoft.AspNetCore.DataProtection; // Thêm using này
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO; // Thêm using này

var builder = WebApplication.CreateBuilder(args);

// === CẤU HÌNH DATABASE ===
builder.Services.AddDbContext<AppDbContext>(options => // Sửa lại tên DbContext nếu cần
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === CẤU HÌNH IDENTITY VÀ COOKIE ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddErrorDescriber<VietnameseIdentityErrorDescriber>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// === CẤU HÌNH CÁC DỊCH VỤ KHÁC ===
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IMomoService, MomoService>();

// === CẤU HÌNH MVC, RAZOR PAGES, SESSION ===
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Tăng thời gian chờ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// === CẤU HÌNH DATA PROTECTION (SỬA LỖI ĐĂNG XUẤT NGẪU NHIÊN) ===
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("MacStoreCloneApp");

// ========================================================
var app = builder.Build();
// ========================================================

// CẤU HÌNH HTTP REQUEST PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Thứ tự Middleware quan trọng
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// CẤU HÌNH ENDPOINT ROUTING
app.MapRazorPages();
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SEED DATABASE
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during seeding");
    }
}

app.Run();