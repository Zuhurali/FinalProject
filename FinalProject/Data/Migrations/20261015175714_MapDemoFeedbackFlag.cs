using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class MapDemoFeedbackFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The MVC migration AddPopularityRankingFields may already have created these columns.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'ProductRatings', N'IsDemo') IS NULL
                    ALTER TABLE ProductRatings ADD IsDemo bit NOT NULL CONSTRAINT DF_ProductRatings_IsDemo DEFAULT (0);
                IF COL_LENGTH(N'ProductReviews', N'IsDemo') IS NULL
                    ALTER TABLE ProductReviews ADD IsDemo bit NOT NULL CONSTRAINT DF_ProductReviews_IsDemo DEFAULT (0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The columns are shared with the MVC migration AddPopularityRankingFields, which removes them.
        }
    }
}
