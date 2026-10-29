using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductFavoriteProductFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve valid favorites; drop only rows that already point at missing products.
            migrationBuilder.Sql("""
                DELETE FROM ProductFavorites
                WHERE NOT EXISTS (
                    SELECT 1 FROM ShopProducts AS sp WHERE sp.Id = ProductFavorites.ProductId);
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductFavorites_ShopProducts_ProductId",
                table: "ProductFavorites",
                column: "ProductId",
                principalTable: "ShopProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductFavorites_ShopProducts_ProductId",
                table: "ProductFavorites");
        }
    }
}
