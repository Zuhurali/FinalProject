using System.Globalization;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Services;
using FinalProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class PartStockEditTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task Stock_update_persists_quantity_preserves_other_fields_and_rejects_negative()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new FinalProjectDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var parts = new PartsAdminService(db);
        var existing = await parts.GetAsync(15);
        Assert.NotNull(existing);
        Assert.True(string.IsNullOrEmpty(existing!.Brand));
        Assert.True(string.IsNullOrEmpty(existing.Compatibility));

        var request = Copy(existing);
        request.Quantity = 11;
        var (updated, error) = await parts.UpdateAsync(15, request);
        Assert.Null(error);
        Assert.Equal(11, updated!.Quantity);

        var stored = await db.ShopProducts.AsNoTracking().SingleAsync(part => part.Id == 15);
        Assert.Equal(11, stored.Quantity);
        Assert.True(stored.InStock);
        Assert.Equal(existing.Name, stored.Name);
        Assert.Equal(existing.Price, stored.Price);
        Assert.Equal(existing.Category, stored.Category);
        Assert.Equal(existing.Description, stored.Description);
        Assert.Equal(existing.ImagePath, stored.ImagePath);
        Assert.Equal(existing.Brand, stored.Brand);
        Assert.Equal(existing.Compatibility, stored.Compatibility);
        Assert.Equal(existing.IsActive, stored.IsActive);

        var inStock = ShopCatalogService.ToModel(CatalogRow(stored));
        Assert.Equal(11, inStock.Quantity);
        Assert.Equal("In Stock", inStock.StockStatus);
        Assert.True(inStock.InStock);
        Assert.Equal("In stock · 11 available", PublicStockText(inStock));

        request.Quantity = 0;
        var (none, noneError) = await parts.UpdateAsync(15, request);
        Assert.Null(noneError);
        Assert.Equal(0, none!.Quantity);
        stored = await db.ShopProducts.AsNoTracking().SingleAsync(part => part.Id == 15);
        Assert.Equal(0, stored.Quantity);
        Assert.False(stored.InStock);
        Assert.Equal("Out of stock", PublicStockText(ShopCatalogService.ToModel(CatalogRow(stored))));

        request.Quantity = 7;
        var (low, lowError) = await parts.UpdateAsync(15, request);
        Assert.Null(lowError);
        Assert.Equal(7, low!.Quantity);
        stored = await db.ShopProducts.AsNoTracking().SingleAsync(part => part.Id == 15);
        Assert.Equal(7, stored.Quantity);
        Assert.True(stored.InStock);
        Assert.Equal("Low Stock", ShowroomRules.StockStatus(stored.Quantity));
        Assert.Equal("Low stock · 7 left", PublicStockText(ShopCatalogService.ToModel(CatalogRow(stored))));

        request.Quantity = -1;
        var (rejected, rejectError) = await parts.UpdateAsync(15, request);
        Assert.Null(rejected);
        Assert.Equal("Stock cannot be negative.", rejectError);
        stored = await db.ShopProducts.AsNoTracking().SingleAsync(part => part.Id == 15);
        Assert.Equal(7, stored.Quantity);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Edit_form_accepts_stock_and_one_active_checkbox_when_optional_text_is_blank()
    {
        var saved = await BindAndValidateAsync(new Dictionary<string, string>
        {
            ["Id"] = "15",
            ["Name"] = "Precision Check Ring",
            ["Price"] = "155.00",
            ["Quantity"] = "11",
            ["Category"] = "Workshop Tools",
            ["Brand"] = "",
            ["Compatibility"] = "",
            ["Description"] = "Precision ring gauge for checking thread",
            ["ImagePath"] = "~/assets/images/portfolio/parts/hd/check-ring.webp",
            ["IsActive"] = "true"
        });

        Assert.True(saved.Valid, saved.Errors);
        Assert.Equal(11, saved.Part.Quantity);
        Assert.Equal("Precision Check Ring", saved.Part.Name);
        Assert.Equal(155.00m, saved.Part.Price);
        Assert.Equal("Workshop Tools", saved.Part.Category);
        Assert.True(string.IsNullOrEmpty(saved.Part.Brand));
        Assert.True(string.IsNullOrEmpty(saved.Part.Compatibility));
        Assert.True(saved.Part.IsActive);
        Assert.Equal("~/assets/images/portfolio/parts/hd/check-ring.webp", saved.Part.ImagePath);

        var uncheckedPart = await BindAndValidateAsync(new Dictionary<string, string>
        {
            ["Id"] = "15",
            ["Name"] = "Precision Check Ring",
            ["Price"] = "155.00",
            ["Quantity"] = "0",
            ["Category"] = "Workshop Tools",
            ["ImagePath"] = "~/assets/images/portfolio/parts/hd/check-ring.webp",
            ["IsActive"] = "false"
        });
        Assert.True(uncheckedPart.Valid, uncheckedPart.Errors);
        Assert.False(uncheckedPart.Part.IsActive);
        Assert.Equal(0, uncheckedPart.Part.Quantity);

        var negative = await BindAndValidateAsync(new Dictionary<string, string>
        {
            ["Id"] = "15",
            ["Name"] = "Precision Check Ring",
            ["Price"] = "155.00",
            ["Quantity"] = "-3",
            ["Category"] = "Workshop Tools",
            ["ImagePath"] = "~/assets/images/portfolio/parts/hd/check-ring.webp",
            ["IsActive"] = "true"
        });
        Assert.False(negative.Valid);
        Assert.Contains("Stock cannot be negative.", negative.Errors);
    }

    [Fact]
    public async Task Create_form_does_not_fail_model_validation_only_because_ImagePath_is_empty()
    {
        // Regression: non-nullable ImagePath was implicitly [Required], so Create failed with
        // "The ImagePath field is required." even after ApplyImageAsync saved the upload.
        var create = await BindAndValidateAsync(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["Name"] = "Cabin Air Filter",
            ["Price"] = "29.99",
            ["Quantity"] = "5",
            ["Category"] = "Filters",
            ["Brand"] = "Bosch",
            ["PartNumber"] = "CAF-100",
            ["CarMake"] = "Toyota",
            ["CarModel"] = "Camry",
            ["Description"] = "Cabin filter for Camry",
            ["ImagePath"] = "",
            ["IsActive"] = "true"
        });

        Assert.True(create.Valid, create.Errors);
        Assert.True(string.IsNullOrEmpty(create.Part.ImagePath));
        Assert.DoesNotContain("ImagePath field is required", create.Errors, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Edit_part_view_has_one_active_checkbox_and_hides_the_image_path()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var edit = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminParts", "Edit.cshtml"));
        var details = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminParts", "Details.cshtml"));

        Assert.Equal(1, Count(edit, "asp-for=\"IsActive\""));
        Assert.DoesNotContain("type=\"checkbox\"", edit, StringComparison.OrdinalIgnoreCase);
        var labelAt = edit.IndexOf("for=\"IsActive\"", StringComparison.Ordinal);
        var inputAt = edit.IndexOf("asp-for=\"IsActive\"", StringComparison.Ordinal);
        Assert.True(labelAt >= 0 && inputAt > labelAt);

        Assert.Contains("enctype=\"multipart/form-data\"", edit, StringComparison.Ordinal);
        Assert.Contains("name=\"image\"", edit, StringComparison.Ordinal);
        Assert.Contains("Url.Content(Model.ImagePath)", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("Current image:", details, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<br />@Model.ImagePath", details, StringComparison.Ordinal);
        Assert.Contains("Url.Content(Model.ImagePath)", details, StringComparison.Ordinal);
    }

    private static async Task<(bool Valid, SavePartDto Part, string Errors)> BindAndValidateAsync(
        Dictionary<string, string> fields)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        await using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
        var form = new FormCollection(fields.ToDictionary(
            pair => pair.Key,
            pair => new Microsoft.Extensions.Primitives.StringValues(pair.Value)));
        var controller = new FormBindingController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ObjectValidator = provider.GetRequiredService<IObjectModelValidator>(),
            ValueProvider = new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture)
        };
        var part = new SavePartDto();
        var bound = await controller.BindAsync(part);
        var errors = string.Join(" ", controller.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage));
        return (bound && controller.ModelState.IsValid, part, errors);
    }

    private static SavePartRequestDto Copy(FinalProject.DTOs.PartAdminItemDto part) => new()
    {
        Id = part.Id,
        Name = part.Name,
        Description = part.Description,
        Price = part.Price,
        Category = part.Category,
        Brand = part.Brand,
        Compatibility = part.Compatibility,
        CarMake = part.CarMake,
        CarModel = part.CarModel,
        Color = part.Color,
        PartNumber = part.PartNumber,
        Quantity = part.Quantity,
        ImagePath = part.ImagePath,
        IsActive = part.IsActive
    };

    private static ShopCatalogProduct CatalogRow(FinalProject.Models.Entities.ShopProductReference part) => new()
    {
        Id = part.Id,
        Name = part.Name,
        Price = part.Price,
        Quantity = part.Quantity,
        Brand = part.Brand,
        Compatibility = part.Compatibility,
        Category = part.Category,
        ImagePath = part.ImagePath,
        Description = part.Description,
        InStock = part.InStock,
        IsActive = part.IsActive
    };

    private static string PublicStockText(ShopProduct product) =>
        product.Quantity <= 0
            ? "Out of stock"
            : product.StockStatus == "Low Stock"
                ? $"Low stock · {product.Quantity} left"
                : $"In stock · {product.Quantity} available";

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private sealed class FormBindingController : Controller
    {
        public IValueProvider ValueProvider { get; set; } = new FormValueProvider(
            BindingSource.Form,
            new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()),
            CultureInfo.InvariantCulture);

        public async Task<bool> BindAsync(SavePartDto part)
        {
            var bound = await TryUpdateModelAsync(part, prefix: string.Empty, ValueProvider);
            TryValidateModel(part);
            return bound;
        }
    }
}
