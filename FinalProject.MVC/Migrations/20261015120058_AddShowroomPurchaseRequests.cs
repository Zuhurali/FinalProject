using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShowroomPurchaseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShowroomPurchaseRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PreferredVisitDate = table.Column<DateTime>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowroomPurchaseRequests", x => x.Id);
                    table.CheckConstraint("CK_ShowroomPurchaseRequests_Quantity", "[Quantity] >= 1 AND [Quantity] <= 10");
                    table.CheckConstraint("CK_ShowroomPurchaseRequests_Status", "[Status] IN (N'New', N'Contacted', N'Completed', N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_ShowroomPurchaseRequests_ShopProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomPurchaseRequests_Email_ProductId_Status",
                table: "ShowroomPurchaseRequests",
                columns: new[] { "Email", "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomPurchaseRequests_ProductId",
                table: "ShowroomPurchaseRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomPurchaseRequests_SubmissionKey",
                table: "ShowroomPurchaseRequests",
                column: "SubmissionKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShowroomPurchaseRequests");
        }
    }
}
