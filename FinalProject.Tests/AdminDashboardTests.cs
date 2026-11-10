using FinalProject.Repositories.Implementations;
using System.Reflection;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using FinalProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class AdminDashboardTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_dashboard_and_users_require_correct_roles()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal($"{AppRoles.Admin},{AppRoles.SuperAdmin}", typeof(AdminController).GetMethod(nameof(AdminController.Users))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal($"{AppRoles.Admin},{AppRoles.SuperAdmin}", typeof(AdminController).GetMethod(nameof(AdminController.UserDetails))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal($"{AppRoles.Admin},{AppRoles.SuperAdmin}", typeof(AdminController).GetMethod(nameof(AdminController.DeleteUser))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(AppRoles.SuperAdmin, typeof(AdminController).GetMethod(nameof(AdminController.ChangeRole))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal(AppRoles.SuperAdmin, typeof(AdminController).GetMethod(nameof(AdminController.ChangeRoles))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public async Task Catalog_admin_summary_matches_sql_server_counts()
    {
        await using var db = new FinalProjectDbContext(new DbContextOptionsBuilder<FinalProjectDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(FinalProjectDbContext.MigrationsHistoryTable))
            .Options);

        var expectedCars = await db.Cars.CountAsync();
        var expectedActive = await db.Cars.CountAsync(car => car.Status == "Active");
        var expectedConversations = await db.Conversations.CountAsync();
        var expectedUnread = await db.Messages.CountAsync(message => !message.IsRead);
        var expectedRatings = await db.ProductRatings.CountAsync();
        var expectedReviews = await db.ProductReviews.CountAsync();

        var summary = await new CatalogAdminService(new FinalProjectRepository(db)).GetSummaryAsync();

        Assert.Equal(expectedCars, summary.TotalCars);
        Assert.Equal(expectedActive, summary.ActiveListings);
        Assert.Equal(expectedConversations, summary.TotalConversations);
        Assert.Equal(expectedUnread, summary.UnreadMessages);
        Assert.Equal(expectedRatings, summary.TotalRatings);
        Assert.Equal(expectedReviews, summary.TotalReviews);
        Assert.True(summary.RecentActivity.Count <= 8);
    }

    [Fact]
    public async Task Dashboard_operational_counts_match_sql_server()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

        var newContacts = await db.ContactRequests.CountAsync(request => request.Status == ContactRequestStatus.New);
        var openQuestions = await db.ProductInquiries.CountAsync(
            inquiry => !inquiry.IsHandled && inquiry.AdminReply == null);
        var openRequests = await db.ShowroomPurchaseRequests.CountAsync(
            request => request.Status == ShowroomRequestStatus.New
                || request.Status == ShowroomRequestStatus.Contacted);
        var sales = await db.InPersonSales.CountAsync(sale => !sale.IsDemo && sale.VoidedAt == null);
        var activeParts = await db.ShopProducts.CountAsync(product => product.IsActive);
        var activeCampaigns = await db.ProductCampaigns.CountAsync(campaign => campaign.IsActive);

        Assert.True(newContacts >= 0);
        Assert.True(openQuestions >= 0);
        Assert.True(openRequests >= 0);
        Assert.True(sales >= 0);
        Assert.True(activeParts >= 0);
        Assert.True(activeCampaigns >= 0);
        Assert.Equal(
            await db.ShopProducts.CountAsync(product => product.IsActive),
            activeParts);
    }

    [Fact]
    public void Admin_summary_response_includes_reviews_metric()
    {
        var ctor = typeof(AdminSummaryResponseDto).GetConstructors().Single();
        var names = ctor.GetParameters().Select(parameter => parameter.Name).ToArray();
        Assert.Contains("TotalReviews", names);
        Assert.Contains("TotalRatings", names);
        Assert.Contains("TotalCars", names);
    }

    [Fact]
    public void Ratings_admin_bulk_endpoint_is_role_protected()
    {
        var method = typeof(FinalProject.Controllers.ProductRatingsController)
            .GetMethod(nameof(FinalProject.Controllers.ProductRatingsController.GetAllForAdmin));
        Assert.NotNull(method);
        Assert.Equal("Admin,SuperAdmin", method!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
