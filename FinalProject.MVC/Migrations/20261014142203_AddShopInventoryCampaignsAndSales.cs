using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShopInventoryCampaignsAndSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "ShopProducts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Compatibility",
                table: "ShopProducts",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ShopProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InPersonSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecordedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SoldAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InPersonSales", x => x.Id);
                    table.CheckConstraint("CK_InPersonSales_Quantity", "[Quantity] >= 1");
                    table.ForeignKey(
                        name: "FK_InPersonSales_ShopProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DiscountedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCampaigns_ShopProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShopProducts_Quantity",
                table: "ShopProducts",
                sql: "[Quantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_InPersonSales_ProductId",
                table: "InPersonSales",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCampaigns_ProductId",
                table: "ProductCampaigns",
                column: "ProductId");

            migrationBuilder.Sql("""
                UPDATE ShopProducts SET Quantity = CASE Id
                    WHEN 1 THEN 24 WHEN 2 THEN 18 WHEN 3 THEN 7 WHEN 4 THEN 3 WHEN 5 THEN 0
                    WHEN 6 THEN 42 WHEN 7 THEN 12 WHEN 8 THEN 4 WHEN 9 THEN 31 WHEN 10 THEN 16
                    WHEN 11 THEN 2 WHEN 12 THEN 9 WHEN 13 THEN 1 WHEN 14 THEN 55 WHEN 15 THEN 0
                    WHEN 16 THEN 22 WHEN 17 THEN 6 WHEN 18 THEN 5 WHEN 19 THEN 11 WHEN 20 THEN 0
                    WHEN 21 THEN 38 ELSE Quantity END,
                    InStock = CASE WHEN CASE Id
                    WHEN 1 THEN 24 WHEN 2 THEN 18 WHEN 3 THEN 7 WHEN 4 THEN 3 WHEN 5 THEN 0
                    WHEN 6 THEN 42 WHEN 7 THEN 12 WHEN 8 THEN 4 WHEN 9 THEN 31 WHEN 10 THEN 16
                    WHEN 11 THEN 2 WHEN 12 THEN 9 WHEN 13 THEN 1 WHEN 14 THEN 55 WHEN 15 THEN 0
                    WHEN 16 THEN 22 WHEN 17 THEN 6 WHEN 18 THEN 5 WHEN 19 THEN 11 WHEN 20 THEN 0
                    WHEN 21 THEN 38 ELSE Quantity END > 0 THEN 1 ELSE 0 END
                WHERE Id BETWEEN 1 AND 21;

                UPDATE ShopProducts SET CarMake = N'', CarModel = N''
                WHERE (Id = 1 AND CarMake = N'Porsche' AND CarModel = N'911')
                   OR (Id = 2 AND CarMake = N'Mercedes-Benz' AND CarModel = N'AMG GT')
                   OR (Id = 3 AND CarMake = N'Toyota' AND CarModel = N'Highlander')
                   OR (Id = 5 AND CarMake = N'Nissan' AND CarModel = N'350Z')
                   OR (Id = 6 AND CarMake = N'Lamborghini' AND CarModel = N'Diablo')
                   OR (Id = 7 AND CarMake = N'Ferrari' AND CarModel = N'California')
                   OR (Id = 8 AND CarMake = N'Porsche' AND CarModel = N'911')
                   OR (Id = 9 AND CarMake = N'Nissan' AND CarModel = N'350Z')
                   OR (Id = 10 AND CarMake = N'Toyota' AND CarModel = N'Highlander')
                   OR (Id = 11 AND CarMake = N'Tesla' AND CarModel = N'Model 3')
                   OR (Id = 12 AND CarMake = N'Maserati' AND CarModel = N'GranTurismo')
                   OR (Id = 13 AND CarMake = N'Mercedes-Benz' AND CarModel = N'SL-Class')
                   OR (Id = 16 AND CarMake = N'Nissan' AND CarModel = N'350Z')
                   OR (Id = 17 AND CarMake = N'Porsche' AND CarModel = N'911')
                   OR (Id = 18 AND CarMake = N'Mercedes-Benz' AND CarModel = N'SL-Class')
                   OR (Id = 19 AND CarMake = N'Toyota' AND CarModel = N'Highlander')
                   OR (Id = 20 AND CarMake = N'Rolls-Royce' AND CarModel = N'Phantom');

                UPDATE ShopProducts SET Category = N'Oil Filters' WHERE Id = 11 AND Name = N'Spin-On Oil Filter';
                UPDATE ShopProducts SET Category = N'Air Filters' WHERE Id = 10 AND Name = N'Pleated Engine Air Filter';
                UPDATE ShopProducts SET Category = N'Brakes' WHERE Id = 8 AND Name = N'Brake Caliper and Rotor';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InPersonSales");

            migrationBuilder.DropTable(
                name: "ProductCampaigns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShopProducts_Quantity",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "Compatibility",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ShopProducts");
        }
    }
}
