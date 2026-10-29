using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddHomePageContentAndCallCta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CallCtaHeadline",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "If you have any questions Please Call.");

            migrationBuilder.CreateTable(
                name: "HomePageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    HeroTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeroHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    HeroImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeroImageAlt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoriesEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CategoriesHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CategoriesHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CategoriesHeadingAfter = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PortfolioEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PortfolioHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PortfolioHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CampaignsEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CampaignsHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CampaignsHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AboutEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AboutHeadingBefore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AboutHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AboutBody = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AboutCtaLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AboutImageMainPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AboutImageFloat1Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AboutImageFloat2Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BrandsEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BrandsHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BrandsHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    WhyChooseEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WhyChooseHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WhyChooseHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    WhyChooseBackgroundPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Feature1Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Feature1Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Feature1IconPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Feature2Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Feature2Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Feature2IconPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Feature3Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Feature3Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Feature3IconPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Feature4Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Feature4Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Feature4IconPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeaturedEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FeaturedHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FeaturedHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePageContents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomePageContents");

            migrationBuilder.DropColumn(
                name: "CallCtaHeadline",
                table: "SiteSettings");
        }
    }
}
