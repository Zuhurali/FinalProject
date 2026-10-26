using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShowroomRequestUserIdAndSaleVoid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ShowroomPurchaseRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "InPersonSales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedByUserId",
                table: "InPersonSales",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomPurchaseRequests_UserId",
                table: "ShowroomPurchaseRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InPersonSales_VoidedAt",
                table: "InPersonSales",
                column: "VoidedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShowroomPurchaseRequests_UserId",
                table: "ShowroomPurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_InPersonSales_VoidedAt",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ShowroomPurchaseRequests");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "InPersonSales");
        }
    }
}
