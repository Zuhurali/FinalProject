using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIconPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconPath",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // Preserve Home strip icons previously assigned by OrderBy(Name) then index % 6.
            migrationBuilder.Sql("""
                ;WITH ordered AS (
                    SELECT [Id], (ROW_NUMBER() OVER (ORDER BY [Name]) - 1) AS [Idx]
                    FROM [Categories]
                )
                UPDATE c
                SET [IconPath] = N'~/assets/images/category/'
                    + RIGHT(N'0' + CAST((o.[Idx] % 6) + 1 AS nvarchar(10)), 2)
                    + N'.svg'
                FROM [Categories] AS c
                INNER JOIN ordered AS o ON o.[Id] = c.[Id];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconPath",
                table: "Categories");
        }
    }
}
