using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FinalProject.Commerce;
using FinalProject.Controllers;
using FinalProject.Models;
using FinalProject.MVC.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FinalProject.Tests;

public class ShowroomRuleTests
{
    [Theory]
    [InlineData(0, "Out of Stock")]
    [InlineData(1, "Low Stock")]
    [InlineData(7, "Low Stock")]
    [InlineData(10, "Low Stock")]
    [InlineData(11, "In Stock")]
    [InlineData(25, "In Stock")]
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
        Assert.Equal("Oct 13", ShowroomRules.InclusiveEndLabel(end));
    }

    [Fact]
    public void Campaign_prices_must_stay_below_the_original()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(2);
        Assert.Null(ShowroomRules.ValidateCampaign(100, 80, null, start, end));
        Assert.Null(ShowroomRules.ValidateCampaign(100, null, 10, start, end));
        Assert.Null(ShowroomRules.ValidateCampaign(100, null, 15, start, end));
        Assert.Null(ShowroomRules.ValidateCampaign(100, null, 100, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, 100, null, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, 80, 10, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, null, 0, start, end));
        Assert.NotNull(ShowroomRules.ValidateCampaign(100, null, 101, start, end));
        Assert.Equal(80m, ShowroomRules.DiscountedPrice(100, null, 20));
        Assert.Equal(1096.50m, ShowroomRules.DiscountedPrice(1290, null, 15));
        Assert.True(ShowroomRules.CampaignWindowsOverlap(start, end, start.AddDays(1), end.AddDays(1)));
        Assert.False(ShowroomRules.CampaignWindowsOverlap(start, end, end, end.AddDays(2)));
        Assert.True(ShowroomRules.CampaignsConflict(true, null, false, 16));
        Assert.True(ShowroomRules.CampaignsConflict(false, 16, true, null));
        Assert.True(ShowroomRules.CampaignsConflict(false, 16, false, 16));
        Assert.False(ShowroomRules.CampaignsConflict(false, 16, false, 17));
        Assert.Equal("Live", ShowroomRules.CampaignPublicStatus(true, start, end, start.AddHours(1)));
        Assert.Equal("Scheduled", ShowroomRules.CampaignPublicStatus(true, start, end, start.AddHours(-1)));
        Assert.Equal("Expired", ShowroomRules.CampaignPublicStatus(true, start, end, end));
        Assert.Equal("Inactive", ShowroomRules.CampaignPublicStatus(false, start, end, start.AddHours(1)));
    }

    [Fact]
    public void Review_content_length_is_validated()
    {
        // Empty / whitespace-only are still rejected.
        Assert.NotNull(ReviewContentRules.Normalize("", out _));
        Assert.NotNull(ReviewContentRules.Normalize("   ", out _));

        // Single character and short text are now accepted (MinLength = 1).
        Assert.Null(ReviewContentRules.Normalize("a", out _));
        Assert.Null(ReviewContentRules.Normalize("short", out _));
        Assert.Null(ReviewContentRules.Normalize("This review is long enough.", out var normalized));
        Assert.Equal("This review is long enough.", normalized);

        // Over-length is still rejected.
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
        var history = typeof(ProductReviewsController).GetMethod(nameof(ProductReviewsController.History))!;
        Assert.Equal("Admin,SuperAdmin", history.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var delete = typeof(ProductReviewsController).GetMethod(nameof(ProductReviewsController.Delete))!;
        Assert.NotNull(delete.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(delete.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var users = typeof(FinalProject.MVC.Controllers.AdminController).GetMethod("Users")!;
        Assert.Equal("SuperAdmin", users.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var changeRoles = typeof(FinalProject.MVC.Controllers.AdminController).GetMethod("ChangeRoles")!;
        Assert.Equal("SuperAdmin", changeRoles.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(changeRoles.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(changeRoles.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        var addFavorite = typeof(FinalProject.MVC.Controllers.CarsController).GetMethod("AddFavorite")!;
        var removeFavorite = typeof(FinalProject.MVC.Controllers.CarsController).GetMethod("RemoveFavorite")!;
        Assert.NotNull(addFavorite.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(removeFavorite.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(addFavorite.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(removeFavorite.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void Parts_page_labels_follow_the_visible_range()
    {
        Assert.Equal((1, 10, 1, 3), PageWindow.Describe(1, 10, 10, 21));
        Assert.Equal((11, 20, 2, 3), PageWindow.Describe(2, 10, 10, 21));
        Assert.Equal((21, 21, 3, 3), PageWindow.Describe(3, 10, 1, 21));
        Assert.Equal((21, 21, 3, 3), PageWindow.Describe(9, 10, 1, 21));
        Assert.Equal((1, 4, 1, 1), PageWindow.Describe(1, 10, 4, 4));
    }

    [Fact]
    public void Member_pages_are_not_the_admin_campaign_surface()
    {
        var controller = typeof(FinalProject.MVC.Controllers.AdminCampaignsController);
        var sales = typeof(FinalProject.MVC.Controllers.AdminSalesController);
        var reviews = typeof(FinalProject.MVC.Controllers.AdminReviewsController);
        Assert.Equal("Admin,SuperAdmin", controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var savePosts = controller.GetMethod("Save")!
            .GetCustomAttributes<HttpPostAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();
        Assert.Contains("Save", savePosts);
        Assert.Contains("Create", savePosts);
        Assert.Equal("Admin,SuperAdmin", sales.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", reviews.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Admin,SuperAdmin", typeof(FinalProject.MVC.Controllers.AdminInquiriesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var parts = typeof(FinalProject.MVC.Controllers.AdminPartsController);
        Assert.Equal("Admin,SuperAdmin", parts.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("SuperAdmin", parts.GetMethod("Create", Type.EmptyTypes)!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Null(parts.GetMethod("Edit", [typeof(int), typeof(CancellationToken)])!.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal(10, FinalProject.MVC.Commerce.ShowroomRules.LowStockMaximum);
        var api = typeof(FinalProject.Controllers.AdminPartsController);
        Assert.Equal("Admin,SuperAdmin", api.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("SuperAdmin", api.GetMethods().Single(method => method.Name == "Create").GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Null(api.GetMethods().Single(method => method.Name == "Update").GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(api.GetMethods().Single(method => method.Name == "Delete").GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(FinalProject.MVC.Security.AccountNameRules.ValidateUsername("ali92"));
        Assert.Null(FinalProject.MVC.Security.AccountNameRules.ValidateUsername("admin"));
        Assert.NotNull(FinalProject.MVC.Security.AccountNameRules.ValidateUsername("ab"));
        Assert.NotNull(FinalProject.MVC.Security.AccountNameRules.ValidateUsername("has space"));
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

    private static FinalProject.MVC.Models.ShowroomPurchaseRequest ValidPurchaseRequest() => new()
    {
        ProductId = 9,
        CustomerName = "Nigar Aliyeva",
        Email = "nigar@example.com",
        Phone = "+994 50 123 45 67",
        Quantity = 2,
        SubmissionKey = Guid.NewGuid()
    };

    [Fact]
    public void Purchase_request_is_valid_for_an_in_stock_product()
    {
        var today = new DateTime(2026, 10, 15);
        var request = ValidPurchaseRequest();
        request.PreferredVisitDate = today.AddDays(3);
        Assert.Null(ShowroomRequestRules.Validate(request, 31, today));
    }

    [Fact]
    public void Purchase_request_is_refused_for_out_of_stock_products()
    {
        Assert.NotNull(ShowroomRequestRules.Validate(ValidPurchaseRequest(), 0, DateTime.Today));
    }

    [Theory]
    [InlineData(0, 31)]
    [InlineData(11, 31)]
    [InlineData(3, 2)]
    public void Purchase_request_quantity_must_fit_the_limit_and_the_stock(int quantity, int available)
    {
        var request = ValidPurchaseRequest();
        request.Quantity = quantity;
        Assert.NotNull(ShowroomRequestRules.Validate(request, available, DateTime.Today));
    }

    [Fact]
    public void Purchase_request_needs_contact_details_a_key_and_a_sensible_date()
    {
        var today = new DateTime(2026, 10, 15);
        var noPhone = ValidPurchaseRequest();
        noPhone.Phone = "12";
        var badEmail = ValidPurchaseRequest();
        badEmail.Email = "nigar";
        var noKey = ValidPurchaseRequest();
        noKey.SubmissionKey = Guid.Empty;
        var past = ValidPurchaseRequest();
        past.PreferredVisitDate = today.AddDays(-1);
        var tooFar = ValidPurchaseRequest();
        tooFar.PreferredVisitDate = today.AddDays(ShowroomRequestRules.MaxVisitDaysAhead + 1);

        Assert.NotNull(ShowroomRequestRules.Validate(noPhone, 31, today));
        Assert.NotNull(ShowroomRequestRules.Validate(badEmail, 31, today));
        Assert.NotNull(ShowroomRequestRules.Validate(noKey, 31, today));
        Assert.NotNull(ShowroomRequestRules.Validate(past, 31, today));
        Assert.NotNull(ShowroomRequestRules.Validate(tooFar, 31, today));
    }

    [Fact]
    public void Purchase_requests_are_reviewed_only_by_admins()
    {
        var controller = typeof(FinalProject.MVC.Controllers.AdminRequestsController);
        Assert.Equal("Admin,SuperAdmin", controller.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        var requestToBuy = typeof(FinalProject.MVC.Controllers.ShopController)
            .GetMethod(nameof(FinalProject.MVC.Controllers.ShopController.RequestToBuy))!;
        Assert.NotNull(requestToBuy.GetCustomAttribute<Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute>());
    }
}
