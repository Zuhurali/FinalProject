using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddContactRequestUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ContactRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_UserId",
                table: "ContactRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactRequests_UserId",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ContactRequests");
        }
    }
}
