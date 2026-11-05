using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ApiPartImagePathRules = FinalProject.Commerce.PartImagePathRules;
using MvcPartImagePathRules = FinalProject.MVC.Commerce.PartImagePathRules;

namespace FinalProject.Tests;

public class SparePartsImageAndCatalogTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Theory]
    [InlineData("~/assets/images/portfolio/11.webp", true)]
    [InlineData("~/assets/images/portfolio/parts/hd/engine-assembly.webp", true)]
    [InlineData("~/assets/images/portfolio/parts/uploads/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg", true)]
    [InlineData("/assets/images/portfolio/parts/x.png", true)]
    [InlineData("https://cdn.example.com/part.jpg", true)]
    [InlineData("C:\\Windows\\System32\\x.png", false)]
    [InlineData("~/assets/images/../secrets.txt", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("", false)]
    public void Part_image_path_allowlist_matches_seeded_and_upload_locations(string path, bool expected)
    {
        Assert.Equal(expected, ApiPartImagePathRules.IsSafeReference(path));
        Assert.Equal(expected, MvcPartImagePathRules.IsSafeReference(path));
    }

    [Fact]
    public async Task Parts_admin_rejects_unsafe_image_paths_and_negative_stock()
    {
        await using var db = CreateApiDb();
        var service = new PartsAdminService(db);
        var unsafeImage = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "Unsafe Path Part",
            Category = "Filters",
            Price = 10,
            Quantity = 1,
            ImagePath = "..\\evil.png",
            IsActive = true
        });
        Assert.Null(unsafeImage.Part);
        Assert.Equal("The product image path is not allowed.", unsafeImage.Error);

        var negative = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "Negative Stock Part",
            Category = "Filters",
            Price = 10,
            Quantity = -1,
            ImagePath = "~/assets/images/portfolio/parts/hd/engine-assembly.webp",
            IsActive = true
        });
        Assert.Null(negative.Part);
        Assert.Equal("Stock cannot be negative.", negative.Error);
    }

    [Fact]
    public async Task Parts_admin_create_persists_sku_fitment_and_safe_image()
    {
        await using var db = CreateApiDb();
        var service = new PartsAdminService(db);
        var (part, error) = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "E2E Cabin Filter",
            Category = "Filters",
            Brand = "Bosch",
            PartNumber = "CAF-E2E-1",
            CarMake = "Toyota",
            CarModel = "Camry",
            Description = "Test filter",
            Price = 29.5m,
            Quantity = 4,
            ImagePath = "~/assets/images/portfolio/parts/uploads/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.webp",
            IsActive = false
        });

        Assert.Null(error);
        Assert.NotNull(part);
        Assert.True(part!.IsActive); // create policy forces active
        Assert.Equal("CAF-E2E-1", part.PartNumber);
        Assert.Equal("Toyota", part.CarMake);
        Assert.Equal("Camry", part.CarModel);

        var listed = await service.ListAsync(null, "Filters", "Bosch", 1, 50);
        Assert.Contains(listed.Items, item => item.Id == part.Id);
        Assert.Contains(listed.Brands, brand => brand == "Bosch");
    }

    [Fact]
    public async Task Inquiry_and_in_store_sale_require_active_product()
    {
        await using var db = CreateMvcDb();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.AsNoTracking().FirstAsync(item => item.IsActive && item.Quantity > 0);
        product = await db.ShopProducts.FirstAsync(item => item.Id == product.Id);
        product.IsActive = false;
        await db.SaveChangesAsync();

        var inquiry = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = product.Id,
            CustomerName = "Tester",
            Email = "tester@example.com",
            Message = "Is this still available for pickup at the Baku store?"
        });
        Assert.False(inquiry.Ok);
        Assert.Equal("Product not found.", inquiry.Error);

        var sale = await catalog.RecordSaleAsync(product.Id, 1, "admin-user", "should fail");
        Assert.False(sale.Ok);
        Assert.Equal("Inactive products cannot be sold in store.", sale.Error);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Sequential_oversell_cannot_reduce_stock_below_zero()
    {
        // RecordSaleAsync opens its own SQL transaction; do not nest an outer test transaction.
        await using var db = CreateMvcDb();
        var catalog = new ShopCatalogService(db);
        var product = await db.ShopProducts.FirstAsync(item => item.IsActive && item.Quantity >= 1);
        var originalQuantity = product.Quantity;
        product.Quantity = 1;
        product.InStock = true;
        product.IsActive = true;
        await db.SaveChangesAsync();

        try
        {
            var first = await catalog.RecordSaleAsync(product.Id, 1, "admin-1", "first-oversell-guard");
            var second = await catalog.RecordSaleAsync(product.Id, 1, "admin-2", "second-oversell-guard");

            Assert.True(first.Ok);
            Assert.False(second.Ok);
            var refreshed = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Equal(0, refreshed.Quantity);
            Assert.False(refreshed.InStock);
        }
        finally
        {
            var restore = await db.ShopProducts.FirstAsync(item => item.Id == product.Id);
            restore.Quantity = originalQuantity;
            restore.InStock = originalQuantity > 0;
            restore.IsActive = true;
            await db.SaveChangesAsync();
            var sales = await db.InPersonSales
                .Where(sale => sale.Note == "first-oversell-guard" || sale.Note == "second-oversell-guard")
                .ToListAsync();
            db.InPersonSales.RemoveRange(sales);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public void Shop_index_exposes_brand_filter_controls()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var shop = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shop", "Index.cshtml"));
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminParts", "Index.cshtml"));

        Assert.Contains("id=\"brand\"", shop, StringComparison.Ordinal);
        Assert.Contains("brand: $('#brand').val()", shop, StringComparison.Ordinal);
        Assert.Contains("name=\"brand\"", admin, StringComparison.Ordinal);
        Assert.Contains("Model.Brands", admin, StringComparison.Ordinal);
    }

    private static FinalProjectDbContext CreateApiDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("parts-image-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static ApplicationDbContext CreateMvcDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}
