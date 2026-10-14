using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinalProject.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AlignAutomotiveProductText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ShopProducts SET Name = N'Gear and Workshop Equipment Set', Category = N'Accessories', Description = N'Gear, shaft, and workshop equipment photographed for the showroom counter.', SearchKeywords = N'Gear and Workshop Equipment Set Accessories' WHERE Name = N'Car Parts Gears and Equipment';
                UPDATE ShopProducts SET Category = N'Brakes', Description = N'Brake, suspension, and filter parts shown together. Fitment is not stated.', SearchKeywords = N'Brake and Suspension Parts Kit Brakes' WHERE Name = N'Brake and Suspension Parts Kit';
                UPDATE ShopProducts SET Name = N'Performance Tire', Category = N'Accessories', Description = N'A performance tire. Vehicle fitment is not stated.', SearchKeywords = N'Performance Tire Accessories' WHERE Name = N'High-Performance Tires';
                UPDATE ShopProducts SET Name = N'Micrometer with Check Ring', Category = N'Workshop Tools', Description = N'Outside micrometer, bore gauges, and check ring.', SearchKeywords = N'Micrometer with Check Ring Workshop Tools' WHERE Name = N'Micrometer with check ring';
                UPDATE ShopProducts SET Name = N'Exhaust Mufflers and Pipes', Category = N'Accessories', Description = N'Exhaust mufflers, pipes, and a turbo assembly. Fitment is not stated.', SearchKeywords = N'Exhaust Mufflers and Pipes Accessories' WHERE Name = N'Car Parts Mufflers And Pipes';
                UPDATE ShopProducts SET Name = N'Machine Gear and Bearing', Category = N'Accessories', Description = N'Machine gear and bearing.', SearchKeywords = N'Machine Gear and Bearing Accessories' WHERE Name = N'Car Gears Machine part';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Complete engine assembly. Vehicle application is not stated.', SearchKeywords = N'Engine Assembly Accessories' WHERE Name = N'Engine Assembly';
                UPDATE ShopProducts SET Description = N'Red brake caliper on a drilled rotor. Pad and vehicle fitment are not stated.', SearchKeywords = N'Brake Caliper and Rotor Brakes' WHERE Name = N'Brake Caliper and Rotor';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Coilover shock absorber with a red spring. Vehicle fitment is not stated.', SearchKeywords = N'Coilover Shock Absorber Accessories' WHERE Name = N'Coilover Shock Absorber';
                UPDATE ShopProducts SET Description = N'Pleated engine air filter. Vehicle fitment is not stated.', SearchKeywords = N'Pleated Engine Air Filter Air Filters' WHERE Name = N'Pleated Engine Air Filter';
                UPDATE ShopProducts SET Description = N'Spin-on oil filter. Vehicle fitment is not stated.', SearchKeywords = N'Spin-On Oil Filter Oil Filters' WHERE Name = N'Spin-On Oil Filter';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Engine timing belt. Vehicle fitment is not stated.', SearchKeywords = N'Timing Belt Accessories' WHERE Name = N'Timing Belt';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Ball bearing. Application is not stated.', SearchKeywords = N'Ball Bearing Accessories' WHERE Name = N'Ball Bearing';
                UPDATE ShopProducts SET Category = N'Workshop Tools', SearchKeywords = N'Bore Gauge Set Workshop Tools' WHERE Name = N'Bore Gauge Set';
                UPDATE ShopProducts SET Category = N'Workshop Tools', SearchKeywords = N'Precision Check Ring Workshop Tools' WHERE Name = N'Precision Check Ring';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Turbocharger and exhaust housing. Vehicle fitment is not stated.', SearchKeywords = N'Turbocharger Assembly Accessories' WHERE Name = N'Turbocharger Assembly';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Exhaust muffler. Vehicle fitment is not stated.', SearchKeywords = N'Exhaust Muffler Accessories' WHERE Name = N'Exhaust Muffler';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Exhaust tip. Vehicle fitment is not stated.', SearchKeywords = N'Exhaust Tip Accessories' WHERE Name = N'Exhaust Tip';
                UPDATE ShopProducts SET Category = N'Accessories', Description = N'Splined driveshaft. Vehicle fitment is not stated.', SearchKeywords = N'Splined Driveshaft Accessories' WHERE Name = N'Splined Driveshaft';
                UPDATE ShopProducts SET Category = N'Accessories', SearchKeywords = N'Gear Shaft and Fastener Kit Accessories' WHERE Name = N'Gear Shaft and Fastener Kit';
                UPDATE ShopProducts SET Category = N'Workshop Tools', SearchKeywords = N'Outside Micrometer Workshop Tools' WHERE Name = N'Outside Micrometer';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
