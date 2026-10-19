using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShopProductIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ShopProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ShopProducts");
        }
    }
}
