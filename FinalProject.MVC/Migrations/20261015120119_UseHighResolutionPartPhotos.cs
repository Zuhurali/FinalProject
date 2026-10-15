using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class UseHighResolutionPartPhotos : Migration
    {
        private static readonly (string OldFile, string NewFile)[] Photos =
        [
            ("engine-assembly.webp", "hd/engine-assembly.webp"),
            ("brake-caliper.webp", "hd/brake-caliper.webp"),
            ("coilover-shock.webp", "hd/coilover-shock.webp"),
            ("air-filter.webp", "hd/air-filter.webp"),
            ("oil-filter.webp", "hd/oil-filter.webp"),
            ("timing-belt.webp", "hd/timing-belt.webp"),
            ("ball-bearing.webp", "hd/ball-bearing.webp"),
            ("bore-gauge.webp", "hd/bore-gauge.webp"),
            ("check-ring.webp", "hd/check-ring.webp"),
            ("turbocharger.webp", "hd/turbocharger.webp"),
            ("exhaust-muffler.webp", "hd/exhaust-muffler.webp"),
            ("exhaust-tip.webp", "hd/exhaust-tip.webp"),
            ("splined-shaft.webp", "hd/splined-shaft.webp"),
            ("gear-shaft.webp", "hd/gear-shaft-fastener-kit.webp"),
            ("outside-micrometer.webp", "hd/outside-micrometer.webp"),
        ];

        private static readonly (string Name, string OldDescription, string NewDescription)[] Descriptions =
        [
            ("Coilover Shock Absorber", "Coilover shock absorber with a red spring. Vehicle fitment is not stated.", "Coilover shock absorber. Vehicle fitment is not stated."),
            ("Bore Gauge Set", "Bore gauge set for engine measurement.", "Inside micrometer set for measuring engine bores."),
            ("Precision Check Ring", "Precision check ring for gauge setup.", "Precision ring gauge for checking threads and setting gauges."),
        ];

        private const string Folder = "~/assets/images/portfolio/parts/";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (oldFile, newFile) in Photos)
            {
                migrationBuilder.Sql($"UPDATE ShopProducts SET ImagePath = N'{Folder}{newFile}' WHERE ImagePath = N'{Folder}{oldFile}';");
            }

            foreach (var (name, oldDescription, newDescription) in Descriptions)
            {
                migrationBuilder.Sql($"UPDATE ShopProducts SET Description = N'{newDescription}' WHERE Name = N'{name}' AND Description = N'{oldDescription}';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (oldFile, newFile) in Photos)
            {
                migrationBuilder.Sql($"UPDATE ShopProducts SET ImagePath = N'{Folder}{oldFile}' WHERE ImagePath = N'{Folder}{newFile}';");
            }

            foreach (var (name, oldDescription, newDescription) in Descriptions)
            {
                migrationBuilder.Sql($"UPDATE ShopProducts SET Description = N'{oldDescription}' WHERE Name = N'{name}' AND Description = N'{newDescription}';");
            }
        }
    }
}
