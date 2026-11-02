using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddHomePortfolioTabContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortfolioEmptyNew",
                table: "HomePageContents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioEmptyStocks",
                table: "HomePageContents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioEmptyTrending",
                table: "HomePageContents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioEmptyUsed",
                table: "HomePageContents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioTabNewLabel",
                table: "HomePageContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioTabStocksLabel",
                table: "HomePageContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioTabTrendingLabel",
                table: "HomePageContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioTabUsedLabel",
                table: "HomePageContents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE HomePageContents
                SET PortfolioTabNewLabel = N'New Cars',
                    PortfolioTabUsedLabel = N'Used Cars',
                    PortfolioTabTrendingLabel = N'Trending Cars',
                    PortfolioTabStocksLabel = N'In Stocks',
                    PortfolioEmptyNew = N'No new cars are currently listed.',
                    PortfolioEmptyUsed = N'No used cars are currently listed.',
                    PortfolioEmptyTrending = N'No cars are currently listed.',
                    PortfolioEmptyStocks = N'No cars are currently in stock.'
                WHERE PortfolioTabNewLabel = N''
                  AND PortfolioTabUsedLabel = N''
                  AND PortfolioTabTrendingLabel = N''
                  AND PortfolioTabStocksLabel = N'';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PortfolioEmptyNew", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioEmptyStocks", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioEmptyTrending", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioEmptyUsed", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioTabNewLabel", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioTabStocksLabel", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioTabTrendingLabel", table: "HomePageContents");
            migrationBuilder.DropColumn(name: "PortfolioTabUsedLabel", table: "HomePageContents");
        }
    }
}
