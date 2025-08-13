using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Ecommerce_WebApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Category> Categories { get; set; }
        // --- Bảng cho Thuộc tính và Giá trị thuộc tính ---
        public DbSet<ProductAttribute> Attributes { get; set; } 
        public DbSet<AttributeValue> AttributeValues { get; set; }

        // --- Bảng cho Sản phẩm và các thành phần liên quan ---
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductSpecification> ProductSpecifications { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        // --- Bảng nối giữa Phiên bản và Giá trị thuộc tính ---
        public DbSet<VariantValue> VariantValues { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Luôn gọi phương thức của lớp cha trước, ĐẶC BIỆT QUAN TRỌNG khi dùng ASP.NET Core Identity.
            base.OnModelCreating(modelBuilder);
            // === CẤU HÌNH KHÓA CHÍNH KẾT HỢP CHO BẢNG NỐI VariantValue ===
            // "Đối với bảng VariantValue, khóa chính của nó KHÔNG PHẢI là một cột,
            // mà là sự kết hợp của hai cột ProductVariantId và AttributeValueId."
            modelBuilder.Entity<VariantValue>()
                .HasKey(vv => new { vv.ProductVariantId, vv.AttributeValueId });

            modelBuilder.Entity<ProductVariant>()
            .Property(pv => pv.Price)
            .HasColumnType("decimal(18, 2)");


            modelBuilder.Entity<Product>()
                .HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade); 


            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Specifications)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình cho ProductVariant và VariantValue
            modelBuilder.Entity<ProductVariant>()
                .HasMany(v => v.VariantValues)
                .WithOne(vv => vv.ProductVariant)
                .HasForeignKey(vv => vv.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
