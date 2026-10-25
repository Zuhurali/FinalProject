using FinalProject.MVC.Data;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class ShopDiscountFilterTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Fifteen_percent_filter_returns_only_live_offer_parts()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        var catalog = new ShopCatalogService(db);
        var shop = await catalog.GetAllAsync();

        var fifteenPercent = shop
            .Where(product => product.HasOffer && product.DiscountPercent == 15)
            .ToList();

        Assert.NotEmpty(fifteenPercent);
        Assert.Contains(fifteenPercent, product => product.Name == "Turbocharger Assembly");
        Assert.All(fifteenPercent, product =>
        {
            Assert.True(product.HasOffer);
            Assert.Equal(15, product.DiscountPercent);
            Assert.True(product.SalePrice < product.Price);
        });

        var none = shop
            .Where(product => product.HasOffer && product.DiscountPercent == 99)
            .ToList();
        Assert.Empty(none);
    }

    [Fact]
    public void Promo_card_links_to_discount_filtered_shop_catalog()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var shop = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "Index.cshtml"));

        Assert.Contains("campaignCatalogUrl", shop, StringComparison.Ordinal);
        Assert.Contains("asp-route-condition=\"used\"", shop, StringComparison.Ordinal);
        Assert.Contains("discount = offerPercent", shop, StringComparison.Ordinal);
        Assert.Contains("class=\"cta-wrapper d-block text-decoration-none\"", shop, StringComparison.Ordinal);
        Assert.DoesNotContain("<div class=\"cta-wrapper\">\r\n                                    <div class=\"image-area jarallax jara-mask-1\">\r\n                                        <img src=\"~/assets/images/cta/01.webp\"", shop, StringComparison.Ordinal);
    }
}
