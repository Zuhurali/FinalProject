using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignScopeAndSaleCampaignFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ProductCampaigns",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "AppliesToAllParts",
                table: "ProductCampaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CampaignDiscountPercent",
                table: "InPersonSales",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "InPersonSales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ListUnitPrice",
                table: "InPersonSales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE InPersonSales
                SET ListUnitPrice = UnitPrice
                WHERE ListUnitPrice = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCampaigns_AppliesToAllParts",
                table: "ProductCampaigns",
                column: "AppliesToAllParts");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductCampaigns_Scope",
                table: "ProductCampaigns",
                sql: "([AppliesToAllParts] = 1 AND [ProductId] IS NULL) OR ([AppliesToAllParts] = 0 AND [ProductId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InPersonSales_CampaignId",
                table: "InPersonSales",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_InPersonSales_ProductCampaigns_CampaignId",
                table: "InPersonSales",
                column: "CampaignId",
                principalTable: "ProductCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InPersonSales_ProductCampaigns_CampaignId",
                table: "InPersonSales");

            migrationBuilder.DropIndex(
                name: "IX_ProductCampaigns_AppliesToAllParts",
                table: "ProductCampaigns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductCampaigns_Scope",
                table: "ProductCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_InPersonSales_CampaignId",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "AppliesToAllParts",
                table: "ProductCampaigns");

            migrationBuilder.DropColumn(
                name: "CampaignDiscountPercent",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "ListUnitPrice",
                table: "InPersonSales");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ProductCampaigns",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
