using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DisplayPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    E164Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WhatsAppDigits = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MapsQuery = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FooterLogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FaviconPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CopyrightText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FacebookUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InstagramUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    YoutubeUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TwitterUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DefaultSeoTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DefaultMetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettings");
        }
    }
}
