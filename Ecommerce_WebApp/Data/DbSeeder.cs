using Ecommerce_WebApp.Data; 
using Microsoft.AspNetCore.Identity;

namespace Ecommerce_WebApp.Data
{
    public static class DbSeeder
    {
        // UserManager sẽ dùng ApplicationUser của bạn
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            var userManager = service.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

            // Tạo các Roles
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("Customer"));

            // Tạo tài khoản Admin mặc định
            var adminUser = new ApplicationUser
            {
                UserName = "admin@macstore.com",
                Email = "admin@macstore.com",
                FullName = "Nguyễn Anh Tài",
                PhoneNumber = "User",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            // Tìm xem admin đã tồn tại trong DB chưa
            var userInDb = await userManager.FindByEmailAsync(adminUser.Email);
            if (userInDb == null)
            {
                // Nếu chưa có, tạo mới với mật khẩu
                await userManager.CreateAsync(adminUser, "Admin@123");
                // Gán Role "Admin" cho tài khoản vừa tạo
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
   