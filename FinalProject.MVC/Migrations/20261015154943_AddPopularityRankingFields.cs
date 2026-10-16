using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPopularityRankingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ShopProducts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "InPersonSales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Existing parts were created before this column. Spread them by id so
            // Latest Items has a real, stable order that is independent of popularity.
            // Product 21 is the newest. New parts set CreatedAt in the catalog service.
            migrationBuilder.Sql(
                """
                UPDATE ShopProducts
                SET CreatedAt = DATEADD(day, Id, CAST('2026-08-01' AS datetime2));

                DECLARE @createdDefault sysname;
                SELECT @createdDefault = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'ShopProducts') AND c.name = N'CreatedAt';
                IF @createdDefault IS NOT NULL
                    EXEC(N'ALTER TABLE ShopProducts DROP CONSTRAINT ' + @createdDefault);

                IF COL_LENGTH(N'ProductRatings', N'IsDemo') IS NULL
                    ALTER TABLE ProductRatings ADD IsDemo bit NOT NULL CONSTRAINT DF_ProductRatings_IsDemo DEFAULT (0);
                IF COL_LENGTH(N'ProductReviews', N'IsDemo') IS NULL
                    ALTER TABLE ProductReviews ADD IsDemo bit NOT NULL CONSTRAINT DF_ProductReviews_IsDemo DEFAULT (0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "InPersonSales");

            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'ProductRatings', N'IsDemo') IS NOT NULL
                BEGIN
                    ALTER TABLE ProductRatings DROP CONSTRAINT DF_ProductRatings_IsDemo;
                    ALTER TABLE ProductRatings DROP COLUMN IsDemo;
                END
                IF COL_LENGTH(N'ProductReviews', N'IsDemo') IS NOT NULL
                BEGIN
                    ALTER TABLE ProductReviews DROP CONSTRAINT DF_ProductReviews_IsDemo;
                    ALTER TABLE ProductReviews DROP COLUMN IsDemo;
                END
                """);
        }
    }
}
