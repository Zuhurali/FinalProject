using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShopProductsAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarMake = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InStock = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fuel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SearchKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopProducts", x => x.Id);
                });

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'SUPERADMIN')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (CONVERT(nvarchar(450), NEWID()), N'SuperAdmin', N'SUPERADMIN', CONVERT(nvarchar(450), NEWID()));

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'ADMIN')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (CONVERT(nvarchar(450), NEWID()), N'Admin', N'ADMIN', CONVERT(nvarchar(450), NEWID()));

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'MEMBER')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (CONVERT(nvarchar(450), NEWID()), N'Member', N'MEMBER', CONVERT(nvarchar(450), NEWID()));

                INSERT INTO AspNetUserRoles (UserId, RoleId)
                SELECT ur.UserId, memberRole.Id
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles legacyRole ON legacyRole.Id = ur.RoleId AND legacyRole.NormalizedName = N'USER'
                INNER JOIN AspNetRoles memberRole ON memberRole.NormalizedName = N'MEMBER'
                WHERE NOT EXISTS (
                    SELECT 1 FROM AspNetUserRoles existing
                    WHERE existing.UserId = ur.UserId AND existing.RoleId = memberRole.Id);

                DELETE ur
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles legacyRole ON legacyRole.Id = ur.RoleId AND legacyRole.NormalizedName = N'USER';

                DELETE FROM AspNetRoles
                WHERE NormalizedName = N'USER'
                  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur WHERE ur.RoleId = AspNetRoles.Id);

                UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopProducts");
        }
    }
}
