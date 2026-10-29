using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerStripAndPageChromeCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandStripEyebrow",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandStripHeadingBefore",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandStripHeadingHighlight",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroCtaBrowseCars",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroCtaBrowseParts",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroCtaVisit",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsedCarsPromoImagePath",
                table: "ShopMarketingContents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsedCarsPromoTag",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsedCarsPromoTitle",
                table: "ShopMarketingContents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTip1ImagePath",
                table: "ShopMarketingContents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTip1Meta",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTip2ImagePath",
                table: "ShopMarketingContents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTip2Meta",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CalculatorPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BreadcrumbTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PageTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IntroText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatorPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DealerPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BreadcrumbTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PageTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaqPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BreadcrumbTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Eyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BannerImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerStripItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerStripItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerStripSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Eyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerStripSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BreadcrumbTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Eyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPageContents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerStripItems_IsPublished",
                table: "PartnerStripItems",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerStripItems_SortOrder",
                table: "PartnerStripItems",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculatorPageContents");

            migrationBuilder.DropTable(
                name: "DealerPageContents");

            migrationBuilder.DropTable(
                name: "FaqPageContents");

            migrationBuilder.DropTable(
                name: "PartnerStripItems");

            migrationBuilder.DropTable(
                name: "PartnerStripSettings");

            migrationBuilder.DropTable(
                name: "TeamPageContents");

            migrationBuilder.DropColumn(
                name: "BrandStripEyebrow",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "BrandStripHeadingBefore",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "BrandStripHeadingHighlight",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "HeroCtaBrowseCars",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "HeroCtaBrowseParts",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "HeroCtaVisit",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "UsedCarsPromoImagePath",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "UsedCarsPromoTag",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "UsedCarsPromoTitle",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitTip1ImagePath",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitTip1Meta",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitTip2ImagePath",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitTip2Meta",
                table: "ShopMarketingContents");
        }
    }
}
