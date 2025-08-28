using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Ecommerce_WebApp.Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddErrorDescriber<VietnameseIdentityErrorDescriber>()  //Báo lỗi tiếng việt cho trang login và register
.AddDefaultTokenProviders();    //hỗ trợ các chức năng như reset password

// CẤU HÌNH GOOGLE AUTHENTICATION
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions => // Thêm nhà cung cấp Google
    {
        // Đọc ClientId và ClientSecret từ file secrets.json 
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 1.Đăng ký cho IEmailSender của Identity.
//    Hệ thống Identity (ví dụ: khi gửi mail xác nhận, quên mật khẩu) sẽ dùng cái này.
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();

// 2. Đăng ký cho IEmailSender của Utility (mà chúng ta tự tạo).
//    OrderController và các Controller khác của chúng ta sẽ dùng cái này.
builder.Services.AddTransient<Ecommerce_WebApp.Utility.IEmailSender, EmailSender>();


builder.Services.AddControllersWithViews();


// Để dùng Razor cho Identity
builder.Services.AddRazorPages();


builder.Services.AddDistributedMemoryCache(); // Cần thiết cho session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Các dòng này đã có sẵn và chính xác. Chúng phải được đặt sau UseRouting.
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Cho Identity UI


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {        
        await Ecommerce_WebApp.Data.DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during seeding");
    }
}


app.Run();