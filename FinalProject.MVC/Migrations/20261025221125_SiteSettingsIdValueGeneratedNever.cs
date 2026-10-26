using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class SiteSettingsIdValueGeneratedNever : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot drop IDENTITY via a simple ALTER; rebuild the empty singleton table.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[SiteSettings]', N'U') IS NOT NULL
                BEGIN
                    CREATE TABLE [SiteSettings_tmp] (
                        [Id] int NOT NULL,
                        [SiteName] nvarchar(120) NOT NULL,
                        [DisplayPhone] nvarchar(40) NOT NULL,
                        [E164Phone] nvarchar(20) NOT NULL,
                        [WhatsAppDigits] nvarchar(20) NOT NULL,
                        [Email] nvarchar(200) NOT NULL,
                        [AddressLine] nvarchar(300) NOT NULL,
                        [MapsQuery] nvarchar(300) NOT NULL,
                        [LogoPath] nvarchar(500) NOT NULL,
                        [FooterLogoPath] nvarchar(500) NOT NULL,
                        [FaviconPath] nvarchar(500) NOT NULL,
                        [FooterText] nvarchar(500) NOT NULL,
                        [CopyrightText] nvarchar(200) NOT NULL,
                        [FacebookUrl] nvarchar(300) NULL,
                        [InstagramUrl] nvarchar(300) NULL,
                        [YoutubeUrl] nvarchar(300) NULL,
                        [TwitterUrl] nvarchar(300) NULL,
                        [DefaultSeoTitle] nvarchar(160) NOT NULL,
                        [DefaultMetaDescription] nvarchar(320) NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_SiteSettings_tmp] PRIMARY KEY ([Id])
                    );

                    INSERT INTO [SiteSettings_tmp] (
                        [Id], [SiteName], [DisplayPhone], [E164Phone], [WhatsAppDigits], [Email],
                        [AddressLine], [MapsQuery], [LogoPath], [FooterLogoPath], [FaviconPath],
                        [FooterText], [CopyrightText], [FacebookUrl], [InstagramUrl], [YoutubeUrl],
                        [TwitterUrl], [DefaultSeoTitle], [DefaultMetaDescription], [UpdatedAt])
                    SELECT
                        [Id], [SiteName], [DisplayPhone], [E164Phone], [WhatsAppDigits], [Email],
                        [AddressLine], [MapsQuery], [LogoPath], [FooterLogoPath], [FaviconPath],
                        [FooterText], [CopyrightText], [FacebookUrl], [InstagramUrl], [YoutubeUrl],
                        [TwitterUrl], [DefaultSeoTitle], [DefaultMetaDescription], [UpdatedAt]
                    FROM [SiteSettings];

                    DROP TABLE [SiteSettings];
                    EXEC sp_rename N'SiteSettings_tmp', N'SiteSettings';
                    EXEC sp_rename N'PK_SiteSettings_tmp', N'PK_SiteSettings', N'OBJECT';
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[SiteSettings]', N'U') IS NOT NULL
                BEGIN
                    CREATE TABLE [SiteSettings_tmp] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [SiteName] nvarchar(120) NOT NULL,
                        [DisplayPhone] nvarchar(40) NOT NULL,
                        [E164Phone] nvarchar(20) NOT NULL,
                        [WhatsAppDigits] nvarchar(20) NOT NULL,
                        [Email] nvarchar(200) NOT NULL,
                        [AddressLine] nvarchar(300) NOT NULL,
                        [MapsQuery] nvarchar(300) NOT NULL,
                        [LogoPath] nvarchar(500) NOT NULL,
                        [FooterLogoPath] nvarchar(500) NOT NULL,
                        [FaviconPath] nvarchar(500) NOT NULL,
                        [FooterText] nvarchar(500) NOT NULL,
                        [CopyrightText] nvarchar(200) NOT NULL,
                        [FacebookUrl] nvarchar(300) NULL,
                        [InstagramUrl] nvarchar(300) NULL,
                        [YoutubeUrl] nvarchar(300) NULL,
                        [TwitterUrl] nvarchar(300) NULL,
                        [DefaultSeoTitle] nvarchar(160) NOT NULL,
                        [DefaultMetaDescription] nvarchar(320) NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_SiteSettings_tmp] PRIMARY KEY ([Id])
                    );

                    SET IDENTITY_INSERT [SiteSettings_tmp] ON;
                    INSERT INTO [SiteSettings_tmp] (
                        [Id], [SiteName], [DisplayPhone], [E164Phone], [WhatsAppDigits], [Email],
                        [AddressLine], [MapsQuery], [LogoPath], [FooterLogoPath], [FaviconPath],
                        [FooterText], [CopyrightText], [FacebookUrl], [InstagramUrl], [YoutubeUrl],
                        [TwitterUrl], [DefaultSeoTitle], [DefaultMetaDescription], [UpdatedAt])
                    SELECT
                        [Id], [SiteName], [DisplayPhone], [E164Phone], [WhatsAppDigits], [Email],
                        [AddressLine], [MapsQuery], [LogoPath], [FooterLogoPath], [FaviconPath],
                        [FooterText], [CopyrightText], [FacebookUrl], [InstagramUrl], [YoutubeUrl],
                        [TwitterUrl], [DefaultSeoTitle], [DefaultMetaDescription], [UpdatedAt]
                    FROM [SiteSettings];
                    SET IDENTITY_INSERT [SiteSettings_tmp] OFF;

                    DROP TABLE [SiteSettings];
                    EXEC sp_rename N'SiteSettings_tmp', N'SiteSettings';
                    EXEC sp_rename N'PK_SiteSettings_tmp', N'PK_SiteSettings', N'OBJECT';
                END
                """);
        }
    }
}
