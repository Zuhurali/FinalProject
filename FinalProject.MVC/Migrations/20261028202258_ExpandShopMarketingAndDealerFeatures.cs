using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class ExpandShopMarketingAndDealerFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogEyebrow",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CatalogHeadingBefore",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CatalogHeadingHighlight",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CatalogIntro",
                table: "ShopMarketingContents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoriesEyebrow",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoriesHeadingBefore",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoriesHeadingHighlight",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactFormTitle",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroImageAlt",
                table: "ShopMarketingContents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                table: "ShopMarketingContents",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "ShopMarketingContents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitEyebrow",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitHeadingBefore",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitHeadingHighlight",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTip1Title",
                table: "ShopMarketingContents",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTip2Title",
                table: "ShopMarketingContents",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why1Description",
                table: "ShopMarketingContents",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why1Title",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why2Description",
                table: "ShopMarketingContents",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why2Title",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why3Description",
                table: "ShopMarketingContents",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why3Title",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why4Description",
                table: "ShopMarketingContents",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Why4Title",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhyChooseEyebrow",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhyChooseHeadingBefore",
                table: "ShopMarketingContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhyChooseHeadingHighlight",
                table: "ShopMarketingContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FeatureList",
                table: "Dealers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [ShopMarketingContents]
                SET
                    [HeroImageAlt] = N'Grey BMW coupe and black Mercedes sedan in the AutoVault store',
                    [SeoTitle] = N'Shop - AutoVault',
                    [SeoDescription] = N'Browse AutoVault spare parts stock in Baku: oils, filters, tires, and workshop supplies for in-store collection.',
                    [CategoriesEyebrow] = N'Parts Category',
                    [CategoriesHeadingBefore] = N'Shop By',
                    [CategoriesHeadingHighlight] = N'Categories',
                    [CatalogEyebrow] = N'Available Stock',
                    [CatalogHeadingBefore] = N'Parts in the',
                    [CatalogHeadingHighlight] = N'Shop',
                    [CatalogIntro] = N'The shop lists photographed automotive parts and workshop supplies for collection at the Baku store. Engine oils, filters, brake parts, plugs, wipers, and fluids can be added by the store when a matching product photo is on file. Fitment is shown only when it is stored for that product.',
                    [WhyChooseEyebrow] = N'Why Choose Us',
                    [WhyChooseHeadingBefore] = N'Why Choose',
                    [WhyChooseHeadingHighlight] = N'Us',
                    [Why1Title] = N'Our Prices',
                    [Why1Description] = N'Prices and stock on this page are the quantities held at Babek Prospekti.',
                    [Why2Title] = N'Inspect before you buy',
                    [Why2Description] = N'Check a part or car online, then inspect it in the store before you pay.',
                    [Why3Title] = N'Available Products',
                    [Why3Description] = N'There is no shipping or courier. Purchases are collected in Baku.',
                    [Why4Title] = N'Staff on site',
                    [Why4Description] = N'Ask our team about fitment. Compatibility is listed only when it is known.',
                    [VisitEyebrow] = N'Before you visit',
                    [VisitHeadingBefore] = N'Plan your',
                    [VisitHeadingHighlight] = N'visit',
                    [VisitTip1Title] = N'See which oils, filters, tires, and spare parts are on the floor',
                    [VisitTip2Title] = N'Used cars are listed separately and are also sold in person',
                    [ContactFormTitle] = N'Get In Touch'
                WHERE [Id] = 1;

                UPDATE [Dealers]
                SET [FeatureList] = N'Engine Type' + CHAR(10) + N'Horsepower' + CHAR(10) + N'Automatic Climate Control' + CHAR(10) + N'Touchscreen Display' + CHAR(10) + N'Navigation System' + CHAR(10) + N'Fog Lights' + CHAR(10) + N'Start-Stop Technology' + CHAR(10) + N'Automatic Headlights'
                WHERE [FeatureList] IS NULL OR LTRIM(RTRIM([FeatureList])) = N'';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogEyebrow",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "CatalogHeadingBefore",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "CatalogHeadingHighlight",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "CatalogIntro",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "CategoriesEyebrow",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "CategoriesHeadingBefore",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "CategoriesHeadingHighlight",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "ContactFormTitle",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "HeroImageAlt",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitEyebrow",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitHeadingBefore",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitHeadingHighlight",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitTip1Title",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "VisitTip2Title",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why1Description",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why1Title",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why2Description",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why2Title",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why3Description",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why3Title",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why4Description",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "Why4Title",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "WhyChooseEyebrow",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "WhyChooseHeadingBefore",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "WhyChooseHeadingHighlight",
                table: "ShopMarketingContents");

            migrationBuilder.DropColumn(
                name: "FeatureList",
                table: "Dealers");
        }
    }
}
