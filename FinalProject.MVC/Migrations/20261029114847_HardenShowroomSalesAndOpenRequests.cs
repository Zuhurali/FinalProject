using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class HardenShowroomSalesAndOpenRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseRequestId",
                table: "InPersonSales",
                type: "int",
                nullable: true);

            // Preserve the newest open request per email+product; cancel older duplicates.
            migrationBuilder.Sql("""
                UPDATE r
                SET Status = N'Cancelled', UpdatedAt = SYSUTCDATETIME()
                FROM ShowroomPurchaseRequests r
                INNER JOIN (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY LOWER(Email), ProductId
                               ORDER BY CreatedAt DESC, Id DESC) AS rn
                    FROM ShowroomPurchaseRequests
                    WHERE Status IN (N'New', N'Contacted')
                ) ranked ON ranked.Id = r.Id
                WHERE ranked.rn > 1;

                UPDATE ShowroomPurchaseRequests
                SET Email = LOWER(Email)
                WHERE Status IN (N'New', N'Contacted')
                  AND Email <> LOWER(Email);
                """);

            migrationBuilder.CreateIndex(
                name: "UX_ShowroomPurchaseRequests_OpenEmailProduct",
                table: "ShowroomPurchaseRequests",
                columns: new[] { "Email", "ProductId" },
                unique: true,
                filter: "[Status] IN (N'New', N'Contacted')");

            migrationBuilder.CreateIndex(
                name: "IX_InPersonSales_PurchaseRequestId",
                table: "InPersonSales",
                column: "PurchaseRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_InPersonSales_ShowroomPurchaseRequests_PurchaseRequestId",
                table: "InPersonSales",
                column: "PurchaseRequestId",
                principalTable: "ShowroomPurchaseRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InPersonSales_ShowroomPurchaseRequests_PurchaseRequestId",
                table: "InPersonSales");

            migrationBuilder.DropIndex(
                name: "UX_ShowroomPurchaseRequests_OpenEmailProduct",
                table: "ShowroomPurchaseRequests");

            migrationBuilder.DropIndex(
                name: "IX_InPersonSales_PurchaseRequestId",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "PurchaseRequestId",
                table: "InPersonSales");
        }
    }
}
