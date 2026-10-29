using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicCmsPagesTeamDealersAndFaqPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnFaqPage",
                table: "FaqItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnShop",
                table: "FaqItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AboutPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PageTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    BreadcrumbTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Eyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HeadingBefore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageLeftPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageRightPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CallCtaLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ShowcaseEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ShowcaseTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShowcaseDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Showcase1Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Showcase1Descriptor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Showcase1ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Showcase2Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Showcase2Descriptor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Showcase2ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Showcase3Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Showcase3Descriptor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Showcase3ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Showcase4Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Showcase4Descriptor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Showcase4ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PageTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FormTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FormIntro = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LocationCardTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PhoneCardTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EmailCardTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MapEmbedUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MapTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CardImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DetailImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DescriptionExtra = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CarType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mileage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FuelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Transmission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Person = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriveType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Engine = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Whatsapp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecTransmission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargingTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatteryCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FastCharge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatteryWarranty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PowertrainWarranty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Displacement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Torque = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Horsepower = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cylinders = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EngineLayout = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EngineConfig = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PrivacyTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PrivacyBody = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    PrivacySeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PrivacySeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TermsTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TermsBody = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    TermsSeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TermsSeoDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopMarketingContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    HeroEyebrow = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeroTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeroText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeroImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FaqSectionEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FaqSectionHeadingBefore = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FaqSectionHeadingHighlight = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopMarketingContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Biography = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_ShowOnShop",
                table: "FaqItems",
                column: "ShowOnShop");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_IsPublished",
                table: "Dealers",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_SortOrder",
                table: "Dealers",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_IsPublished",
                table: "TeamMembers",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_SortOrder",
                table: "TeamMembers",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutPageContents");

            migrationBuilder.DropTable(
                name: "ContactPageContents");

            migrationBuilder.DropTable(
                name: "Dealers");

            migrationBuilder.DropTable(
                name: "LegalPageContents");

            migrationBuilder.DropTable(
                name: "ShopMarketingContents");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_FaqItems_ShowOnShop",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "ShowOnFaqPage",
                table: "FaqItems");

            migrationBuilder.DropColumn(
                name: "ShowOnShop",
                table: "FaqItems");
        }
    }
}
