using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce_WebApp.Migrations
{
    /// <inheritdoc />
    public partial class ThemTruongAnhChoBangCATE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "Categories");
        }
    }
}
