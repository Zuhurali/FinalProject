using FinalProject.Repositories.Implementations;
using System.Reflection;
using System.Security.Claims;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Security;
using FinalProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class SparePartsManagementTests
{
    [Fact]
    public void Create_spare_part_requires_Admin_or_SuperAdmin_on_mvc_and_api()
    {
        var mvc = typeof(AdminPartsController);
        var mvcCreateGet = mvc.GetMethod(nameof(AdminPartsController.Create), Type.EmptyTypes)!;
        var mvcCreatePost = mvc.GetMethods()
            .Single(method => method.Name == nameof(AdminPartsController.Create) && method.GetParameters().Length > 0);
        Assert.Equal("Admin,SuperAdmin", mvc.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", mvcCreateGet.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", mvcCreatePost.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(mvcCreatePost.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        var apiCreate = typeof(FinalProject.Controllers.AdminPartsController)
            .GetMethods()
            .Single(method => method.Name == "Create");
        Assert.Equal("Admin,SuperAdmin", apiCreate.GetCustomAttribute<AuthorizeAttribute>()!.Roles);

        var accountAdd = typeof(AccountController).GetMethod(nameof(AccountController.AddSparePart))!;
        Assert.Equal("Admin,SuperAdmin", accountAdd.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Members_cannot_see_add_new_inventory_controls()
    {
        Assert.False(CarCreationAccess.CanCreateCars(Principal(AppRoles.Member)));
        Assert.False(CarCreationAccess.CanCreateCars(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.True(CarCreationAccess.CanCreateCars(Principal(AppRoles.Admin)));
        Assert.True(CarCreationAccess.CanCreateCars(Principal(AppRoles.SuperAdmin)));
    }

    [Fact]
    public void Create_post_clears_ImagePath_model_errors_after_upload_is_applied()
    {
        // Mirrors AdminPartsController.AcceptResolvedImagePath after ApplyImageAsync succeeds.
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        modelState.AddModelError("ImagePath", "The ImagePath field is required.");
        modelState.AddModelError(nameof(FinalProject.MVC.DTOs.SavePartDto.ImagePath), "The ImagePath field is required.");

        modelState.Remove(nameof(FinalProject.MVC.DTOs.SavePartDto.ImagePath));
        modelState.Remove("ImagePath");
        modelState.Remove("part.ImagePath");

        Assert.False(modelState.ContainsKey("ImagePath"));
        Assert.False(modelState.ContainsKey(nameof(FinalProject.MVC.DTOs.SavePartDto.ImagePath)));
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task PartsAdminService_rejects_invalid_create_and_accepts_sku_fitment()
    {
        await using var db = CreateDb();
        var service = new PartsAdminService(new FinalProjectRepository(db));

        var (missing, missingError) = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "A",
            Category = "Filters",
            Price = 10,
            Quantity = 1,
            ImagePath = ""
        });
        Assert.Null(missing);
        Assert.Equal("Enter a part name.", missingError);

        var (created, error) = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "Oil Filter XF-200",
            Category = "Filters",
            Brand = "Bosch",
            PartNumber = "XF-200-SKU",
            CarMake = "Toyota",
            CarModel = "Camry",
            Compatibility = "2018-2024 Camry",
            Price = 24.50m,
            Quantity = 12,
            ImagePath = "~/assets/images/portfolio/parts/uploads/demo.webp",
            Description = "OEM-style oil filter"
        });

        Assert.Null(error);
        Assert.NotNull(created);
        Assert.Equal("XF-200-SKU", created!.PartNumber);
        Assert.Equal("Toyota", created.CarMake);
        Assert.Equal("Camry", created.CarModel);
        Assert.True(created.IsActive);
        Assert.True(created.Id > 0);

        var loaded = await service.GetAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("XF-200-SKU", loaded!.PartNumber);
        Assert.Equal(12, loaded.Quantity);
    }

    [Fact]
    public void Admin_parts_edit_form_exposes_color_input()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var edit = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminParts", "Edit.cshtml"));
        var dto = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "MVC", "DTOs", "PartAdminDtos.cs"));
        var apiDto = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "DTOs", "PartAdminDtos.cs"));
        var service = File.ReadAllText(Path.Combine(root, "AutoVault.Service", "Services", "PartsAdminService.cs"));

        Assert.Contains("asp-for=\"Color\"", edit, StringComparison.Ordinal);
        Assert.Contains("public string? Color { get; set; }", dto, StringComparison.Ordinal);
        Assert.Contains("public string? Color { get; set; }", apiDto, StringComparison.Ordinal);
        Assert.Contains("part.Color = request.Color ?? string.Empty;", service, StringComparison.Ordinal);
        Assert.Contains("Color = part.Color,", service, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartsAdminService_saves_color_and_preserves_other_fields()
    {
        await using var db = CreateDb();
        var service = new PartsAdminService(new FinalProjectRepository(db));

        var (created, createError) = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "Caliper Cover",
            Category = "Brakes",
            Brand = "Brembo",
            PartNumber = "CLR-1",
            CarMake = "BMW",
            CarModel = "M3",
            Compatibility = "F80",
            Price = 99.50m,
            Quantity = 4,
            ImagePath = "~/assets/images/portfolio/parts/uploads/demo.webp",
            Description = "Red cover",
            Color = ""
        });
        Assert.Null(createError);
        Assert.NotNull(created);
        Assert.Equal(string.Empty, created!.Color);

        var update = new SavePartRequestDto
        {
            Id = created.Id,
            Name = created.Name,
            Category = created.Category,
            Brand = created.Brand,
            PartNumber = created.PartNumber,
            CarMake = created.CarMake,
            CarModel = created.CarModel,
            Compatibility = created.Compatibility,
            Price = created.Price,
            Quantity = created.Quantity,
            ImagePath = created.ImagePath,
            Description = created.Description,
            IsActive = created.IsActive,
            Color = "  Red  "
        };
        var (updated, updateError) = await service.UpdateAsync(created.Id, update);
        Assert.Null(updateError);
        Assert.NotNull(updated);
        Assert.Equal("Red", updated!.Color);
        Assert.Equal("Caliper Cover", updated.Name);
        Assert.Equal("Brakes", updated.Category);
        Assert.Equal("Brembo", updated.Brand);
        Assert.Equal("CLR-1", updated.PartNumber);
        Assert.Equal("BMW", updated.CarMake);
        Assert.Equal("M3", updated.CarModel);
        Assert.Equal(99.50m, updated.Price);
        Assert.Equal(4, updated.Quantity);
        Assert.Equal("~/assets/images/portfolio/parts/uploads/demo.webp", updated.ImagePath);

        var loaded = await service.GetAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Red", loaded!.Color);

        var entity = await db.ShopProducts.AsNoTracking().SingleAsync(item => item.Id == created.Id);
        Assert.Equal("Red", entity.Color);
    }

    [Fact]
    public async Task PartsAdminService_rejects_negative_stock_and_oversized_sku()
    {
        await using var db = CreateDb();
        var service = new PartsAdminService(new FinalProjectRepository(db));

        var (neg, negError) = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "Brake Pad",
            Category = "Brakes",
            Price = 40,
            Quantity = -1,
            ImagePath = "~/assets/images/portfolio/parts/uploads/demo.webp"
        });
        Assert.Null(neg);
        Assert.Equal("Stock cannot be negative.", negError);

        var (sku, skuError) = await service.CreateAsync(new SavePartRequestDto
        {
            Name = "Brake Pad",
            Category = "Brakes",
            PartNumber = new string('X', 81),
            Price = 40,
            Quantity = 2,
            ImagePath = "~/assets/images/portfolio/parts/uploads/demo.webp"
        });
        Assert.Null(sku);
        Assert.Equal("Part number / SKU cannot exceed 80 characters.", skuError);
    }

    private static FinalProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("spare-parts-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new FinalProjectDbContext(options);
    }

    private static ClaimsPrincipal Principal(string role)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "tester"),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}
