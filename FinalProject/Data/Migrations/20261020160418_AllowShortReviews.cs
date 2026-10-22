using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowShortReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductReviews_ContentLength",
                table: "ProductReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductReviews_ContentLength",
                table: "ProductReviews",
                sql: "LEN([Content]) >= 1 AND LEN([Content]) <= 1000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductReviews_ContentLength",
                table: "ProductReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductReviews_ContentLength",
                table: "ProductReviews",
                sql: "LEN([Content]) >= 10 AND LEN([Content]) <= 1000");
        }
    }
}
