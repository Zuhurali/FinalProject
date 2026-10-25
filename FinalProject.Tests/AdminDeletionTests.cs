using System.Reflection;
using FinalProject.Data;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinalProject.Tests;

public class AdminDeletionTests
{
    [Fact]
    public void Admin_delete_actions_require_Admin_or_SuperAdmin_and_post_with_antiforgery()
    {
        AssertAuthorizedPostDelete(typeof(AdminReviewsController), nameof(AdminReviewsController.Delete));
        AssertAuthorizedPostDelete(typeof(AdminRequestsController), nameof(AdminRequestsController.Delete));
        AssertAuthorizedPostDelete(typeof(AdminInquiriesController), nameof(AdminInquiriesController.Delete));
        AssertAuthorizedPostDelete(typeof(AdminContactRequestsController), nameof(AdminContactRequestsController.Delete));

        var apiAdminDelete = typeof(FinalProject.Controllers.ProductReviewsController)
            .GetMethod(nameof(FinalProject.Controllers.ProductReviewsController.AdminDelete))!;
        Assert.Equal("Admin,SuperAdmin", apiAdminDelete.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(apiAdminDelete.GetCustomAttribute<HttpDeleteAttribute>());
        Assert.Null(apiAdminDelete.GetCustomAttribute<HttpGetAttribute>());
        Assert.Null(apiAdminDelete.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task DeletePurchaseRequestAsync_removes_request_only_and_keeps_product()
    {
        await using var db = CreateShopDb();
        var catalog = new ShopCatalogService(db);
        var product = await SeedProductAsync(db);

        var created = await catalog.SavePurchaseRequestAsync(new ShowroomPurchaseRequest
        {
            ProductId = product.Id,
            CustomerName = "Nigar Aliyeva",
            Email = "nigar-delete@example.com",
            Phone = "+994 50 123 45 67",
            Quantity = 1,
            SubmissionKey = Guid.NewGuid()
        });
        Assert.True(created.Ok, created.Error);
        Assert.NotNull(created.RequestId);

        var saleCountBefore = await db.InPersonSales.CountAsync();
        Assert.True(await catalog.DeletePurchaseRequestAsync(created.RequestId!.Value));

        Assert.Empty(await db.ShowroomPurchaseRequests.Where(item => item.Id == created.RequestId).ToListAsync());
        Assert.NotNull(await db.ShopProducts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == product.Id));
        Assert.Equal(saleCountBefore, await db.InPersonSales.CountAsync());
        Assert.False(await catalog.DeletePurchaseRequestAsync(created.RequestId.Value));
        Assert.False(await catalog.DeletePurchaseRequestAsync(99999));
    }

    [Fact]
    public async Task DeleteInquiryAsync_removes_question_only_and_keeps_product()
    {
        await using var db = CreateShopDb();
        var catalog = new ShopCatalogService(db);
        var product = await SeedProductAsync(db);

        var (saved, saveError) = await catalog.SaveInquiryAsync(new ProductInquiry
        {
            ProductId = product.Id,
            CustomerName = "Reply Delete Check",
            Email = "inquiry-delete@example.com",
            Message = "Is this part still available in the showroom today?"
        });
        Assert.True(saved, saveError);

        var inquiry = await db.ProductInquiries.AsNoTracking()
            .SingleAsync(item => item.Email == "inquiry-delete@example.com");

        Assert.True(await catalog.DeleteInquiryAsync(inquiry.Id));
        Assert.Empty(await db.ProductInquiries.Where(item => item.Id == inquiry.Id).ToListAsync());
        Assert.NotNull(await db.ShopProducts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == product.Id));
        Assert.False(await catalog.DeleteInquiryAsync(inquiry.Id));
        Assert.False(await catalog.DeleteInquiryAsync(99999));
    }

    [Fact]
    public async Task DeleteReviewAsAdminAsync_removes_review_and_revisions_without_ownership()
    {
        var options = new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseInMemoryDatabase("admin-review-delete-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new FinalProjectDbContext(options);
        db.ShopProducts.Add(new FinalProject.Models.Entities.ShopProductReference
        {
            Id = 42,
            Name = "Admin Delete Filter",
            Price = 10m,
            Quantity = 5,
            Category = "Filters",
            ImagePath = "/assets/images/parts/filter.webp",
            Description = "Test part",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.IdentityUsers.Add(new FinalProject.Models.Entities.IdentityUserReference
        {
            Id = "member-user",
            UserName = "member",
            Email = "member@example.com",
            FirstName = "Member",
            LastName = "User"
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var reviews = new ProductReviewService(db, config);

        var (created, createError) = await reviews.SaveReviewAsync(42, null, "member-user", "Original admin-delete review.");
        Assert.True(created != null, createError);
        var (updated, updateError) = await reviews.SaveReviewAsync(42, created!.Id, "member-user", "Edited admin-delete review.");
        Assert.True(updated != null, updateError);
        Assert.NotEmpty(await db.ProductReviewRevisions.Where(item => item.ReviewId == created.Id).ToListAsync());

        Assert.True(await reviews.DeleteReviewAsAdminAsync(created.Id));
        Assert.Empty(await db.ProductReviews.Where(item => item.Id == created.Id).ToListAsync());
        Assert.Empty(await db.ProductReviewRevisions.Where(item => item.ReviewId == created.Id).ToListAsync());
        Assert.NotNull(await db.ShopProducts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == 42));
        Assert.NotNull(await db.IdentityUsers.AsNoTracking().SingleOrDefaultAsync(user => user.Id == "member-user"));
        Assert.False(await reviews.DeleteReviewAsAdminAsync(created.Id));
        Assert.False(await reviews.DeleteReviewAsAdminAsync(99999));
    }

    private static void AssertAuthorizedPostDelete(Type controllerType, string methodName)
    {
        Assert.Equal("Admin,SuperAdmin", controllerType.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var delete = controllerType.GetMethod(methodName)!;
        Assert.NotNull(delete.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(delete.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(delete.GetCustomAttribute<HttpGetAttribute>());
        Assert.Null(delete.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    private static ApplicationDbContext CreateShopDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("admin-delete-shop-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<ShopCatalogProduct> SeedProductAsync(ApplicationDbContext db)
    {
        var product = new ShopCatalogProduct
        {
            Name = "Admin Delete Test Part",
            Price = 25m,
            Quantity = 12,
            Brand = "AutoVault",
            Category = "Filters",
            ImagePath = "/assets/images/parts/filter.webp",
            Description = "Seeded for admin deletion tests.",
            InStock = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.ShopProducts.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
