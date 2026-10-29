using System.Reflection;
using FinalProject.MVC.Commerce;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalProject.Tests;

public class ContactRequestPersistenceTests
{
    [Fact]
    public async Task SaveAsync_persists_valid_request_as_New()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);
        var model = ValidModel();

        var (ok, id, errors, error) = await service.SaveAsync(model);

        Assert.True(ok, error);
        Assert.NotNull(id);
        Assert.Empty(errors);
        var stored = await db.ContactRequests.AsNoTracking().SingleAsync(item => item.Id == id);
        Assert.Equal("Ada Lovelace", stored.CustomerName);
        Assert.Equal("ada@example.com", stored.Email);
        Assert.Equal("+994 50 354 67 67", stored.Phone);
        Assert.Equal("Brake pads", stored.Interest);
        Assert.Equal("Please call me about availability this week.", stored.Message);
        Assert.Equal(ContactRequestStatus.New, stored.Status);
        Assert.Null(stored.UserId);
        Assert.True(stored.CreatedAt <= DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task SaveAsync_stores_authenticated_user_id()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);

        var (ok, id, _, error) = await service.SaveAsync(ValidModel(), "member-user-42");

        Assert.True(ok, error);
        var stored = await db.ContactRequests.AsNoTracking().SingleAsync(item => item.Id == id);
        Assert.Equal("member-user-42", stored.UserId);
    }

    [Fact]
    public async Task GetForUserAsync_returns_only_that_members_requests()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);

        await service.SaveAsync(ValidModel(), "user-a");
        await service.SaveAsync(ValidModel("other@example.com", "Second request message here."), "user-a");
        await service.SaveAsync(ValidModel("b@example.com", "Belonging to another member user."), "user-b");
        await service.SaveAsync(ValidModel("guest@example.com", "Anonymous guest submission text."));

        var mine = await service.GetForUserAsync("user-a");

        Assert.Equal(2, mine.Count);
        Assert.All(mine, item => Assert.Equal("user-a", item.UserId));
    }

    [Fact]
    public async Task SaveAsync_rejects_invalid_phone_without_persisting()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);
        var model = ValidModel();
        model.Phone = "123abc";

        var (ok, id, errors, error) = await service.SaveAsync(model);

        Assert.False(ok);
        Assert.Null(id);
        Assert.Equal(PhoneNumberValidation.ErrorMessage, errors["phone"]);
        Assert.Equal("Please correct the highlighted fields.", error);
        Assert.Equal(0, await db.ContactRequests.CountAsync());
    }

    [Fact]
    public async Task SetStatusAsync_updates_allowed_statuses_only()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);
        var (ok, id, _, error) = await service.SaveAsync(ValidModel());
        Assert.True(ok, error);

        Assert.True((await service.SetStatusAsync(id!.Value, ContactRequestStatus.InProgress)).Ok);
        Assert.True((await service.SetStatusAsync(id.Value, ContactRequestStatus.Resolved)).Ok);
        Assert.False((await service.SetStatusAsync(id.Value, "Contacted")).Ok);
        Assert.False((await service.SetStatusAsync(id.Value, ContactRequestStatus.New)).Ok);
        Assert.False((await service.SetStatusAsync(99999, ContactRequestStatus.New)).Ok);

        var stored = await db.ContactRequests.AsNoTracking().SingleAsync(item => item.Id == id);
        Assert.Equal(ContactRequestStatus.Resolved, stored.Status);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Theory]
    [InlineData(ContactRequestStatus.New, ContactRequestStatus.InProgress, true)]
    [InlineData(ContactRequestStatus.New, ContactRequestStatus.Resolved, true)]
    [InlineData(ContactRequestStatus.InProgress, ContactRequestStatus.Resolved, true)]
    [InlineData(ContactRequestStatus.Resolved, ContactRequestStatus.New, false)]
    [InlineData(ContactRequestStatus.Resolved, ContactRequestStatus.InProgress, false)]
    [InlineData(ContactRequestStatus.InProgress, ContactRequestStatus.New, false)]
    public void Contact_status_transitions_follow_workflow(string from, string to, bool allowed)
    {
        Assert.Equal(allowed, ContactRequestStatus.CanTransition(from, to));
    }

    [Fact]
    public void Admin_contact_requests_require_Admin_or_SuperAdmin()
    {
        var authorize = typeof(AdminContactRequestsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin,SuperAdmin", authorize!.Roles);

        var status = typeof(AdminContactRequestsController).GetMethod(nameof(AdminContactRequestsController.Status))!;
        Assert.NotNull(status.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(status.GetCustomAttribute<HttpPostAttribute>());

        var delete = typeof(AdminContactRequestsController).GetMethod(nameof(AdminContactRequestsController.Delete))!;
        Assert.NotNull(delete.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(delete.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(delete.GetCustomAttribute<HttpGetAttribute>());
        Assert.Null(delete.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Member_account_has_no_contact_request_delete_endpoint()
    {
        Assert.Null(typeof(AccountController).GetMethod("DeleteContactRequest"));
        Assert.Null(typeof(AccountController).GetMethod("Delete"));
        var memberPage = typeof(AccountController).GetMethod(nameof(AccountController.ContactRequests))!;
        Assert.Null(memberPage.GetCustomAttribute<HttpPostAttribute>());
    }

    [Fact]
    public void Member_contact_requests_page_requires_authorization_not_admin_role()
    {
        var method = typeof(AccountController).GetMethod(nameof(AccountController.ContactRequests))!;
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrEmpty(authorize!.Roles));
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public async Task ClaimOrphanedByVerifiedEmailAsync_links_only_matching_email()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);

        await service.SaveAsync(ValidModel("member@gmail.com", "Orphan matching member email."));
        await service.SaveAsync(ValidModel("asdad@gmail.com", "Orphan with unrelated guest email."));
        await service.SaveAsync(ValidModel("other@example.com", "Already owned by someone else."), "other-user");

        var claimed = await service.ClaimOrphanedByVerifiedEmailAsync("member-user-1", "Member@Gmail.com");

        Assert.Equal(1, claimed);
        var mine = await service.GetForUserAsync("member-user-1");
        Assert.Single(mine);
        Assert.Equal("member@gmail.com", mine[0].Email);

        var unrelated = await db.ContactRequests.AsNoTracking()
            .SingleAsync(item => item.Email == "asdad@gmail.com");
        Assert.Null(unrelated.UserId);

        var owned = await db.ContactRequests.AsNoTracking()
            .SingleAsync(item => item.Email == "other@example.com");
        Assert.Equal("other-user", owned.UserId);
    }

    [Fact]
    public async Task ClaimOrphanedByVerifiedEmailAsync_does_not_attach_unrelated_emails()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);
        await service.SaveAsync(ValidModel("asdad@gmail.com", "Guest submission not owned by anyone."));

        var claimed = await service.ClaimOrphanedByVerifiedEmailAsync(
            "d9ed280d-7ee2-46b0-8436-68e581adc981",
            "member@gmail.com");

        Assert.Equal(0, claimed);
        Assert.Empty(await service.GetForUserAsync("d9ed280d-7ee2-46b0-8436-68e581adc981"));
        Assert.Null((await db.ContactRequests.AsNoTracking().SingleAsync()).UserId);
    }

    [Fact]
    public void Contact_send_requires_antiforgery_and_posts_json()
    {
        var send = typeof(ContactController).GetMethod(nameof(ContactController.Send))!;
        Assert.NotNull(send.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(send.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal("application/json", send.GetCustomAttribute<ProducesAttribute>()!.ContentTypes.Single());
    }

    [Fact]
    public async Task DeleteAsync_removes_request_from_admin_and_member_lists()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);

        var (ok, id, _, error) = await service.SaveAsync(ValidModel("owner@example.com", "Member owned request to remove."), "owner-1");
        Assert.True(ok, error);
        await service.SaveAsync(ValidModel("keep@example.com", "Unrelated request that must remain."), "other-user");

        Assert.True(await service.DeleteAsync(id!.Value));

        Assert.DoesNotContain(await service.GetAllAsync(), item => item.Id == id);
        Assert.Empty(await service.GetForUserAsync("owner-1"));
        Assert.Single(await service.GetAllAsync());
        Assert.Single(await service.GetForUserAsync("other-user"));
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_missing_or_already_deleted_id()
    {
        await using var db = CreateDb();
        var service = new ContactRequestService(db);
        var (ok, id, _, error) = await service.SaveAsync(ValidModel());
        Assert.True(ok, error);

        Assert.True(await service.DeleteAsync(id!.Value));
        Assert.False(await service.DeleteAsync(id.Value));
        Assert.False(await service.DeleteAsync(99999));
        Assert.Equal(0, await db.ContactRequests.CountAsync());
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("contact-requests-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ContactFormModel ValidModel(
        string email = "ada@example.com",
        string message = "Please call me about availability this week.") => new()
    {
        Name = "Ada Lovelace",
        Email = email,
        Phone = "+994 50 354 67 67",
        Car = "Brake pads",
        Message = message
    };
}
