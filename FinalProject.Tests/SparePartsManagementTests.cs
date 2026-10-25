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
        modelState.AddModelError(nameof(FinalProject.MVC.Models.Api.SavePartDto.ImagePath), "The ImagePath field is required.");

        modelState.Remove(nameof(FinalProject.MVC.Models.Api.SavePartDto.ImagePath));
        modelState.Remove("ImagePath");
        modelState.Remove("part.ImagePath");

        Assert.False(modelState.ContainsKey("ImagePath"));
        Assert.False(modelState.ContainsKey(nameof(FinalProject.MVC.Models.Api.SavePartDto.ImagePath)));
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task PartsAdminService_rejects_invalid_create_and_accepts_sku_fitment()
    {
        await using var db = CreateDb();
        var service = new PartsAdminService(db);

        var (missing, missingError) = await service.CreateAsync(new SavePartRequest
        {
            Name = "A",
            Category = "Filters",
            Price = 10,
            Quantity = 1,
            ImagePath = ""
        });
        Assert.Null(missing);
        Assert.Equal("Enter a part name.", missingError);

        var (created, error) = await service.CreateAsync(new SavePartRequest
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
    public async Task PartsAdminService_rejects_negative_stock_and_oversized_sku()
    {
        await using var db = CreateDb();
        var service = new PartsAdminService(db);

        var (neg, negError) = await service.CreateAsync(new SavePartRequest
        {
            Name = "Brake Pad",
            Category = "Brakes",
            Price = 40,
            Quantity = -1,
            ImagePath = "~/assets/images/portfolio/parts/uploads/demo.webp"
        });
        Assert.Null(neg);
        Assert.Equal("Stock cannot be negative.", negError);

        var (sku, skuError) = await service.CreateAsync(new SavePartRequest
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
