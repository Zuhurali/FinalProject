using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryAdminReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminRepliedAt",
                table: "ProductInquiries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "ProductInquiries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminUserId",
                table: "ProductInquiries",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminRepliedAt",
                table: "ProductInquiries");

            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "ProductInquiries");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "ProductInquiries");
        }
    }
}
