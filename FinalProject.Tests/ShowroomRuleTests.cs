using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FinalProject.Commerce;
using FinalProject.Controllers;
using FinalProject.Models;
using FinalProject.MVC.Commerce;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace FinalProject.Tests;

public class ShowroomRuleTests
{
    [Theory]
    [InlineData(0, "Out of Stock")]
    [InlineData(1, "Low Stock")]
    [InlineData(5, "Low Stock")]
    [InlineData(6, "In Stock")]
    [InlineData(60, "In Stock")]
    public void StockStatus_follows_quantity_boundaries(int quantity, string expected)
    {
        Assert.Equal(expected, ShowroomRules.StockStatus(quantity));
    }

    [Fact]
    public void StockStatus_rejects_negative_quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShowroomRules.StockStatus(-1));
    }

    [Fact]
    public void Sale_cannot_exceed_available_quantity()
    {
        Assert.Null(ShowroomRules.ValidateSaleQuantity(5, 5));
        Assert.NotNull(ShowroomRules.ValidateSaleQuantity(6, 5));
        Assert.NotNull(ShowroomRules.ValidateSaleQuantity(0, 5));
        Assert.NotNull(ShowroomRules.ValidateSaleQuantity(1, 0));
    }

    [Fact]
    public void Expired_or_inactive_campaigns_are_hidden()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(ShowroomRules.OfferIsVisible(true, start, end, start));
        Assert.False(ShowroomRules.OfferIsVisible(true, start, end, end));
        Assert.False(ShowroomRules.OfferIsVisible(false, start, end, start.AddDays(1)));
    }

    [Fact]
    public void Campaign_prices_must_stay_below_the_original()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(2);
        Assert.Null(ShowroomRules.ValidateCampaign(100, 80, null, start, end));
        Assert.Null(ShowroomRules.ValidateCampaign(100, null, 10, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, 100, null, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, 80, 10, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, null, 95, start, end));
        Assert.Equal(80m, ShowroomRules.DiscountedPrice(100, null, 20));
    }

    [Fact]
    public void Review_content_length_is_validated()
    {
        Assert.NotNull(ReviewContentRules.Normalize("short", out _));
        Assert.Null(ReviewContentRules.Normalize("This review is long enough.", out var normalized));
        Assert.Equal("This review is long enough.", normalized);
        Assert.NotNull(ReviewContentRules.Normalize(new string('a', 1001), out _));
    }

    [Fact]
    public void Rating_request_accepts_only_one_to_five()
    {
        var valid = new SaveProductRatingRequest { Rating = 5 };
        var invalid = new SaveProductRatingRequest { Rating = 0 };
        Assert.True(Validator.TryValidateObject(valid, new ValidationContext(valid), null, true));
        Assert.False(Validator.TryValidateObject(invalid, new ValidationContext(invalid), null, true));
    }

    [Fact]
    public void Review_writes_require_an_authenticated_user()
    {
        var createMethod = typeof(ProductReviewsController).GetMethod(nameof(ProductReviewsController.Create))!;
        var update = typeof(ProductReviewsController).GetMethod(nameof(ProductReviewsController.Update))!;
        var moderate = typeof(ProductReviewsController).GetMethod(nameof(ProductReviewsController.Moderate))!;
        Assert.NotNull(createMethod.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(createMethod.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(update!.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("Admin,SuperAdmin", moderate!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Member_pages_are_not_the_admin_campaign_surface()
    {
        var controller = typeof(FinalProject.MVC.Controllers.AdminCampaignsController);
        var sales = typeof(FinalProject.MVC.Controllers.AdminSalesController);
        var reviews = typeof(FinalProject.MVC.Controllers.AdminReviewsController);
        Assert.Equal("Admin,SuperAdmin", controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", sales.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", reviews.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(FinalProject.MVC.Controllers.AdminInquiriesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Product_question_requires_a_real_message()
    {
        var inquiry = new FinalProject.MVC.Models.ProductInquiry
        {
            ProductId = 1,
            CustomerName = "A",
            Email = "not-an-email",
            Message = "short"
        };
        Assert.NotNull(FinalProject.MVC.Commerce.ProductInquiryRules.Validate(inquiry));
        inquiry.CustomerName = "Nigar Aliyeva";
        inquiry.Email = "nigar@example.com";
        inquiry.Message = "Does this oil filter fit a 2019 BMW 3 Series?";
        Assert.Null(FinalProject.MVC.Commerce.ProductInquiryRules.Validate(inquiry));
    }
}
