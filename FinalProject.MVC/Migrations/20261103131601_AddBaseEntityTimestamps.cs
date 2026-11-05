using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseEntityTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var catalogEpoch = new DateTime(2026, 10, 2, 0, 0, 0, 0, DateTimeKind.Utc);

            void AddCreatedAt(string table) =>
                migrationBuilder.AddColumn<DateTime>(
                    name: "CreatedAt",
                    table: table,
                    type: "datetime2",
                    nullable: false,
                    defaultValue: catalogEpoch);

            void AddUpdatedAt(string table) =>
                migrationBuilder.AddColumn<DateTime>(
                    name: "UpdatedAt",
                    table: table,
                    type: "datetime2",
                    nullable: true);

            // CMS / settings rows already had UpdatedAt: backfill CreatedAt from it after add.
            foreach (var cmsTable in new[]
                     {
                         "TeamPageContents",
                         "SiteSettings",
                         "ShopMarketingContents",
                         "PartnerStripSettings",
                         "LegalPageContents",
                         "HomePageContents",
                         "FaqPageContents",
                         "DealerPageContents",
                         "ContactPageContents",
                         "CalculatorPageContents",
                         "AboutPageContents"
                     })
            {
                AddCreatedAt(cmsTable);
                migrationBuilder.Sql($"UPDATE [{cmsTable}] SET [CreatedAt] = [UpdatedAt];");
            }

            AddUpdatedAt("ShopProducts");
            AddUpdatedAt("ProductInquiries");
            AddUpdatedAt("Coupons");

            AddCreatedAt("ProductCampaigns");
            AddUpdatedAt("ProductCampaigns");
            migrationBuilder.Sql("UPDATE ProductCampaigns SET CreatedAt = StartsAt;");

            AddCreatedAt("Orders");
            AddUpdatedAt("Orders");
            migrationBuilder.Sql("UPDATE Orders SET CreatedAt = OrderDate;");

            AddCreatedAt("OrderItems");
            AddUpdatedAt("OrderItems");
            migrationBuilder.Sql(@"
UPDATE oi
SET oi.CreatedAt = o.OrderDate
FROM OrderItems oi
INNER JOIN Orders o ON o.Id = oi.OrderId;");

            AddCreatedAt("InPersonSales");
            AddUpdatedAt("InPersonSales");
            migrationBuilder.Sql("UPDATE InPersonSales SET CreatedAt = SoldAt;");

            AddCreatedAt("CouponUsages");
            AddUpdatedAt("CouponUsages");
            migrationBuilder.Sql("UPDATE CouponUsages SET CreatedAt = UsedAt;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TeamPageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductInquiries");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProductCampaigns");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductCampaigns");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PartnerStripSettings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LegalPageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InPersonSales");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HomePageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FaqPageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DealerPageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CouponUsages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CouponUsages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ContactPageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CalculatorPageContents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AboutPageContents");
        }
    }
}
