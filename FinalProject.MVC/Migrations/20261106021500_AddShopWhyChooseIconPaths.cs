using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShopWhyChooseIconPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddIcon(migrationBuilder, "Why1IconPath", "~/assets/images/why-choose/icon-05.svg");
            AddIcon(migrationBuilder, "Why2IconPath", "~/assets/images/why-choose/icon-06.svg");
            AddIcon(migrationBuilder, "Why3IconPath", "~/assets/images/why-choose/icon-07.svg");
            AddIcon(migrationBuilder, "Why4IconPath", "~/assets/images/why-choose/icon-08.svg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Why1IconPath", table: "ShopMarketingContents");
            migrationBuilder.DropColumn(name: "Why2IconPath", table: "ShopMarketingContents");
            migrationBuilder.DropColumn(name: "Why3IconPath", table: "ShopMarketingContents");
            migrationBuilder.DropColumn(name: "Why4IconPath", table: "ShopMarketingContents");
        }

        private static void AddIcon(MigrationBuilder migrationBuilder, string name, string existingPath)
        {
            migrationBuilder.AddColumn<string>(
                name: name,
                table: "ShopMarketingContents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: existingPath);

            migrationBuilder.AlterColumn<string>(
                name: name,
                table: "ShopMarketingContents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldDefaultValue: existingPath);
        }
    }
}
