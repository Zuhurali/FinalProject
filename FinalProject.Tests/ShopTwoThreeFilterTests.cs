using FinalProject.MVC.Repositories.Implementations;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class ShopTwoThreeFilterTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void ShopTwo_category_chips_come_from_viewbag_not_hardcoded_list()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var shopTwo = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "ShopTwo.cshtml"));
        var shopController = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "ShopController.cs"));

        Assert.Contains("ViewBag.Categories", shopTwo, StringComparison.Ordinal);
        Assert.Contains("data-category=\"@category\"", shopTwo, StringComparison.Ordinal);
        Assert.DoesNotContain("data-category=\"Brakes\"", shopTwo, StringComparison.Ordinal);
        Assert.DoesNotContain("data-category=\"Filters\"", shopTwo, StringComparison.Ordinal);
        Assert.DoesNotContain("data-category=\"Suspension\"", shopTwo, StringComparison.Ordinal);
        Assert.DoesNotContain("data-category=\"Lighting\"", shopTwo, StringComparison.Ordinal);
        Assert.Contains("ViewBag.Categories = _allProducts", shopController, StringComparison.Ordinal);
    }

    [Fact]
    public void ShopThree_posts_selected_colors_and_filter_request_exposes_them()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var shopThree = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "ShopThree.cshtml"));
        var shopModel = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "MVC", "Models", "ShopProduct.cs"));
        var shopController = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "ShopController.cs"));
        var catalog = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "MVC", "Services", "ShopCatalogService.cs"));

        Assert.Contains("selectedColors: selectedColors", shopThree, StringComparison.Ordinal);
        Assert.Contains("public List<string>? SelectedColors { get; set; }", shopModel, StringComparison.Ordinal);
        Assert.Contains("Apply selected colors filter", shopController, StringComparison.Ordinal);
        Assert.Contains("request.SelectedColors", shopController, StringComparison.Ordinal);
        Assert.Contains("string.Equals(p.Color, color, StringComparison.OrdinalIgnoreCase)", shopController, StringComparison.Ordinal);
        Assert.Contains("Color = product.Color", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Catalog_categories_match_sql_shop_products()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        var catalog = new ShopCatalogService(new ApplicationRepository(db));
        var products = (await catalog.GetAllAsync()).ToList();

        Assert.NotEmpty(products);

        var categories = products
            .Select(product => product.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(categories);
        Assert.Contains(categories, category => category.Equals("Brakes", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(categories, category => category.Equals("Suspension", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(categories, category => category.Equals("Lighting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Color_filter_predicate_matches_stored_values_and_clears_with_empty_selection()
    {
        var products = new List<ShopProduct>
        {
            new() { Id = 1, Name = "A", Color = "Black", Category = "Brakes" },
            new() { Id = 2, Name = "B", Color = "Silver", Category = "Brakes" },
            new() { Id = 3, Name = "C", Color = "White", Category = "Accessories" },
            new() { Id = 4, Name = "D", Color = "", Category = "Accessories" }
        };

        static List<ShopProduct> Apply(IEnumerable<ShopProduct> source, IReadOnlyList<string>? selectedColors)
        {
            var filtered = source.AsEnumerable();
            if (selectedColors != null && selectedColors.Count > 0)
            {
                var colors = selectedColors
                    .Where(color => !string.IsNullOrWhiteSpace(color))
                    .Select(color => color.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (colors.Count > 0)
                {
                    filtered = filtered.Where(product =>
                        colors.Any(color =>
                            string.Equals(product.Color, color, StringComparison.OrdinalIgnoreCase)));
                }
            }

            return filtered.ToList();
        }

        var blackOnly = Apply(products, new[] { "Black" });
        Assert.Single(blackOnly);
        Assert.Equal(1, blackOnly[0].Id);

        var multi = Apply(products, new[] { "Black", "Silver" });
        Assert.Equal(2, multi.Count);

        var cleared = Apply(products, Array.Empty<string>());
        Assert.Equal(4, cleared.Count);

        var nullSelection = Apply(products, null);
        Assert.Equal(4, nullSelection.Count);
    }

    [Fact]
    public void ShopFilterRequest_selected_colors_default_null_supports_clearing()
    {
        var request = new ShopFilterRequest();
        Assert.Null(request.SelectedColors);

        request.SelectedColors = new List<string> { "Black", "Silver" };
        Assert.Equal(2, request.SelectedColors.Count);

        request.SelectedColors = new List<string>();
        Assert.Empty(request.SelectedColors);
    }
}
