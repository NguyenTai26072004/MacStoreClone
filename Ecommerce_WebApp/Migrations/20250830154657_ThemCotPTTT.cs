using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce_WebApp.Migrations
{
    /// <inheritdoc />
    public partial class ThemCotPTTT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "OrderHeaders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "OrderHeaders");
        }
    }
}
