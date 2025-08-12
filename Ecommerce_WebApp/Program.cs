using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

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


// Đăng ký dịch vụ EmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();


builder.Services.AddControllersWithViews();

// Để dùng Razor cho Identity
builder.Services.AddRazorPages(); 

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