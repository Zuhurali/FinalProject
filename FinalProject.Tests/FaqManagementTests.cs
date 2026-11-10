using FinalProject.MVC.Repositories.Implementations;
using System.ComponentModel.DataAnnotations;
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

[Collection("CampaignSql")]
public class FaqManagementTests
{
    private const string ConnectionString =
        "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Admin_faq_surfaces_require_Admin_or_SuperAdmin_and_antiforgery()
    {
        Assert.Equal("Admin,SuperAdmin", typeof(AdminFaqsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(AdminFaqsController).GetMethod(nameof(AdminFaqsController.Create), [typeof(FaqItemEditViewModel), typeof(CancellationToken)])!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(typeof(AdminFaqsController).GetMethod(nameof(AdminFaqsController.Delete))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(typeof(AdminFaqsController).GetMethod(nameof(AdminFaqsController.Publish))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(typeof(AdminFaqsController).GetMethod(nameof(AdminFaqsController.Move))!
            .GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void View_model_min_lengths_match_database_check_constraints()
    {
        Assert.Equal(3, FaqContentRules.QuestionMinLength);
        Assert.Equal(300, FaqContentRules.QuestionMaxLength);
        Assert.Equal(3, FaqContentRules.AnswerMinLength);
        Assert.Equal(4000, FaqContentRules.AnswerMaxLength);

        var questionMin = typeof(FaqItemEditViewModel).GetProperty(nameof(FaqItemEditViewModel.Question))!
            .GetCustomAttribute<MinLengthAttribute>()!;
        var answerMin = typeof(FaqItemEditViewModel).GetProperty(nameof(FaqItemEditViewModel.Answer))!
            .GetCustomAttribute<MinLengthAttribute>()!;
        Assert.Equal(FaqContentRules.QuestionMinLength, questionMin.Length);
        Assert.Equal(FaqContentRules.AnswerMinLength, answerMin.Length);

        Assert.Equal(
            "Question must be at least 3 characters.",
            FaqContentRules.ValidateQuestion("ne"));
        Assert.Equal(
            "Answer must be at least 3 characters.",
            FaqContentRules.ValidateAnswer("ok"));
        Assert.Null(FaqContentRules.ValidateQuestion("Why?"));
        Assert.Null(FaqContentRules.ValidateAnswer("Yes."));
    }

    [Fact]
    public async Task Create_rejects_too_short_question_before_database_round_trip()
    {
        await using var db = CreateDb();
        var before = await db.FaqItems.CountAsync();
        var service = new FaqService(new ApplicationRepository(db));

        var (ok, error, id) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = "ne",
            Answer = "ok",
            SortOrder = 1,
            IsPublished = true
        });

        Assert.False(ok);
        Assert.Null(id);
        Assert.Contains("at least 3", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, await db.FaqItems.CountAsync());
    }

    [Fact]
    public async Task Create_rejects_blank_question_and_answer()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var (ok, error, _) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = " ",
            Answer = " ",
            SortOrder = 1,
            IsPublished = true
        });
        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task Faq_crud_publish_order_and_public_filter_persist_on_sql_server()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"FAQ-{Guid.NewGuid():N}";

        var (created, createError, id) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} published?",
            Answer = "Yes, this answer is published for tests.",
            SortOrder = 10,
            IsPublished = true
        });
        Assert.True(created, createError);
        Assert.NotNull(id);

        var (createdHidden, hiddenError, hiddenId) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} hidden?",
            Answer = "This answer must not appear publicly.",
            SortOrder = 20,
            IsPublished = false
        });
        Assert.True(createdHidden, hiddenError);
        Assert.NotNull(hiddenId);

        var published = await service.GetPublishedAsync();
        Assert.Contains(published, item => item.Id == id);
        Assert.DoesNotContain(published, item => item.Id == hiddenId);
        Assert.True(published.Select(item => item.SortOrder).SequenceEqual(published.Select(item => item.SortOrder).OrderBy(value => value)));

        var moved = await service.MoveAsync(id!.Value, 1);
        Assert.True(moved.Ok, moved.Error);

        var updated = await service.UpdateAsync(new FaqItemEditViewModel
        {
            Id = id.Value,
            Question = $"{marker} updated?",
            Answer = "Updated published answer.",
            SortOrder = 15,
            IsPublished = true
        });
        Assert.True(updated.Ok, updated.Error);

        Assert.True(await service.SetPublishedAsync(id.Value, false));
        published = await service.GetPublishedAsync();
        Assert.DoesNotContain(published, item => item.Id == id);

        Assert.True(await service.DeleteAsync(id.Value));
        Assert.True(await service.DeleteAsync(hiddenId!.Value));
        Assert.Null(await service.GetByIdAsync(id.Value));
    }

    [Fact]
    public async Task Valid_faq_round_trip_lists_publishes_and_appears_on_public_query()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"VALID-{Guid.NewGuid():N}";

        var (ok, error, id) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} How do I visit the showroom?",
            Answer = "Call or WhatsApp the store to schedule a visit.",
            SortOrder = 0,
            IsPublished = true
        });
        Assert.True(ok, error);
        Assert.True(id > 0);

        var adminList = await service.GetAllAsync();
        Assert.Contains(adminList, item => item.Id == id);

        var publicList = await service.GetPublishedAsync();
        Assert.Contains(publicList, item => item.Id == id && item.Question.Contains(marker, StringComparison.Ordinal));

        Assert.True(await service.SetPublishedAsync(id!.Value, false));
        Assert.DoesNotContain(await service.GetPublishedAsync(), item => item.Id == id);

        Assert.True(await service.DeleteAsync(id.Value));
    }

    [Fact]
    public async Task Move_up_and_down_persist_and_update_public_order()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"MOVE-{Guid.NewGuid():N}";

        var first = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} First question?",
            Answer = "First answer text.",
            SortOrder = 10,
            IsPublished = true
        });
        var second = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} Second question?",
            Answer = "Second answer text.",
            SortOrder = 20,
            IsPublished = true
        });
        var third = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} Third question?",
            Answer = "Third answer text.",
            SortOrder = 30,
            IsPublished = true
        });
        Assert.True(first.Ok, first.Error);
        Assert.True(second.Ok, second.Error);
        Assert.True(third.Ok, third.Error);

        var down = await service.MoveAsync(first.Id!.Value, 1);
        Assert.True(down.Ok, down.Error);

        var adminOrder = (await service.GetAllAsync())
            .Where(item => item.Question.StartsWith(marker, StringComparison.Ordinal))
            .Select(item => item.Id)
            .ToList();
        Assert.Equal(new[] { second.Id!.Value, first.Id.Value, third.Id!.Value }, adminOrder);

        var publicOrder = (await service.GetPublishedAsync())
            .Where(item => item.Question.StartsWith(marker, StringComparison.Ordinal))
            .Select(item => item.Id)
            .ToList();
        Assert.Equal(adminOrder, publicOrder);

        var up = await service.MoveAsync(first.Id.Value, -1);
        Assert.True(up.Ok, up.Error);

        adminOrder = (await service.GetAllAsync())
            .Where(item => item.Question.StartsWith(marker, StringComparison.Ordinal))
            .Select(item => item.Id)
            .ToList();
        Assert.Equal(new[] { first.Id.Value, second.Id.Value, third.Id.Value }, adminOrder);

        await using (var verifyDb = CreateDb())
        {
            var persisted = await verifyDb.FaqItems.AsNoTracking()
                .Where(item => item.Question.StartsWith(marker))
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id)
                .Select(item => item.Id)
                .ToListAsync();
            Assert.Equal(adminOrder, persisted);
        }

        Assert.True(await service.DeleteAsync(first.Id.Value));
        Assert.True(await service.DeleteAsync(second.Id.Value));
        Assert.True(await service.DeleteAsync(third.Id.Value));
    }

    [Fact]
    public async Task Move_at_boundaries_is_rejected_and_duplicate_orders_still_reorder()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"BOUND-{Guid.NewGuid():N}";

        var a = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} Alpha question?",
            Answer = "Alpha answer text.",
            SortOrder = 5,
            IsPublished = true
        });
        var b = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} Beta question?",
            Answer = "Beta answer text.",
            SortOrder = 5,
            IsPublished = true
        });
        Assert.True(a.Ok, a.Error);
        Assert.True(b.Ok, b.Error);

        var all = (await service.GetAllAsync()).ToList();
        var upBlocked = await service.MoveAsync(all[0].Id, -1);
        Assert.False(upBlocked.Ok);
        Assert.Contains("top", upBlocked.Error, StringComparison.OrdinalIgnoreCase);

        var downBlocked = await service.MoveAsync(all[^1].Id, 1);
        Assert.False(downBlocked.Ok);
        Assert.Contains("bottom", downBlocked.Error, StringComparison.OrdinalIgnoreCase);

        var ours = all.Where(item => item.Question.StartsWith(marker, StringComparison.Ordinal))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToList();
        var firstOurs = ours[0];
        var secondOurs = ours[1];

        var moved = await service.MoveAsync(firstOurs.Id, 1);
        Assert.True(moved.Ok, moved.Error);

        var reordered = (await service.GetAllAsync())
            .Where(item => item.Question.StartsWith(marker, StringComparison.Ordinal))
            .Select(item => item.Id)
            .ToList();
        Assert.Equal(new[] { secondOurs.Id, firstOurs.Id }, reordered);

        var orders = (await service.GetAllAsync())
            .Where(item => item.Question.StartsWith(marker, StringComparison.Ordinal))
            .Select(item => item.SortOrder)
            .ToList();
        Assert.True(orders[0] < orders[1]);

        Assert.True(await service.DeleteAsync(a.Id!.Value));
        Assert.True(await service.DeleteAsync(b.Id!.Value));
    }

    [Fact]
    public async Task Publishing_uses_IsPublished_as_the_only_public_faq_visibility_rule()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"PUB-{Guid.NewGuid():N}";

        var (ok, error, id) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} Can I collect this part?",
            Answer = "Yes. Visit the Baku store.",
            SortOrder = 40,
            IsPublished = false,
            ShowOnShop = true
        });
        Assert.True(ok, error);

        // Legacy column may be false; public /Faq must still follow IsPublished only.
        var entity = await db.FaqItems.FirstAsync(item => item.Id == id);
        entity.ShowOnFaqPage = false;
        await db.SaveChangesAsync();

        Assert.DoesNotContain(await service.GetPublishedAsync(), item => item.Id == id);
        Assert.DoesNotContain(await service.GetShopPublishedAsync(), item => item.Id == id);

        Assert.True(await service.SetPublishedAsync(id!.Value, true));
        var published = await service.GetByIdAsync(id.Value);
        Assert.NotNull(published);
        Assert.True(published!.IsPublished);
        Assert.True(published.ShowOnShop);
        Assert.False(published.ShowOnFaqPage);
        Assert.Contains(await service.GetPublishedAsync(), item => item.Id == id);
        Assert.Contains(await service.GetShopPublishedAsync(), item => item.Id == id);

        Assert.True(await service.SetPublishedAsync(id.Value, false));
        var unpublished = await service.GetByIdAsync(id.Value);
        Assert.NotNull(unpublished);
        Assert.False(unpublished!.IsPublished);
        Assert.True(unpublished.ShowOnShop);
        Assert.False(unpublished.ShowOnFaqPage);
        Assert.DoesNotContain(await service.GetPublishedAsync(), item => item.Id == id);
        Assert.DoesNotContain(await service.GetShopPublishedAsync(), item => item.Id == id);

        Assert.True(await service.DeleteAsync(id.Value));
    }

    [Fact]
    public async Task Repeated_publish_and_unpublish_keeps_public_faq_in_sync()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"RPT-{Guid.NewGuid():N}";
        var (ok, error, id) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} How do I book a visit?",
            Answer = "Call the Baku showroom to schedule a time.",
            SortOrder = 45,
            IsPublished = false,
            ShowOnShop = false
        });
        Assert.True(ok, error);

        for (var round = 0; round < 3; round++)
        {
            Assert.True(await service.SetPublishedAsync(id!.Value, true));
            Assert.Contains(await service.GetPublishedAsync(), item => item.Id == id);

            Assert.True(await service.SetPublishedAsync(id.Value, false));
            Assert.DoesNotContain(await service.GetPublishedAsync(), item => item.Id == id);
        }

        var stored = await service.GetByIdAsync(id!.Value);
        Assert.NotNull(stored);
        Assert.False(stored!.IsPublished);
        Assert.False(stored.ShowOnShop);

        Assert.True(await service.DeleteAsync(id.Value));
    }

    [Fact]
    public async Task Edit_keeps_shop_visibility_independent_of_public_faq_publish_state()
    {
        await using var db = CreateDb();
        var service = new FaqService(new ApplicationRepository(db));
        var marker = $"EDIT-{Guid.NewGuid():N}";
        var (ok, error, id) = await service.CreateAsync(new FaqItemEditViewModel
        {
            Question = $"{marker} Where is the store?",
            Answer = "Babek Prospekti, Baku.",
            SortOrder = 50,
            IsPublished = true,
            ShowOnShop = true
        });
        Assert.True(ok, error);

        var saved = await service.UpdateAsync(new FaqItemEditViewModel
        {
            Id = id!.Value,
            Question = $"{marker} Where is the store?",
            Answer = "Babek Prospekti, Baku.",
            SortOrder = 50,
            IsPublished = true,
            ShowOnShop = false
        });
        Assert.True(saved.Ok, saved.Error);

        var stored = await service.GetByIdAsync(id.Value);
        Assert.NotNull(stored);
        Assert.True(stored!.IsPublished);
        Assert.False(stored.ShowOnShop);
        Assert.Contains(await service.GetPublishedAsync(), item => item.Id == id);
        Assert.DoesNotContain(await service.GetShopPublishedAsync(), item => item.Id == id);

        Assert.True(await service.DeleteAsync(id.Value));
    }

    [Fact]
    public void Admin_faq_form_has_no_separate_faq_visibility_checkbox()
    {
        var root = FindRepoRoot();
        var layout = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Shared", "_AdminLayout.cshtml"));
        var form = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminFaqs", "_FaqForm.cshtml"));
        var index = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminFaqs", "Index.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Controllers", "AdminFaqsController.cs"));

        Assert.Contains("input.form-check-input ~ label::before", layout, StringComparison.Ordinal);
        Assert.Contains("content: none !important;", layout, StringComparison.Ordinal);
        Assert.Equal(1, Count(form, "<input asp-for=\"IsPublished\""));
        Assert.Equal(1, Count(form, "<input asp-for=\"ShowOnShop\""));
        Assert.DoesNotContain("ShowOnFaqPage", form, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowOnFaqPage", typeof(FaqItemEditViewModel).GetProperties().Select(p => p.Name));
        Assert.Contains("bool.TryParse(publish", controller, StringComparison.Ordinal);
        Assert.Contains("value=\"@(item.IsPublished ? \"false\" : \"true\")\"", index, StringComparison.Ordinal);
        Assert.Contains("badge bg-success\">Published", index, StringComparison.Ordinal);
        Assert.Contains("badge bg-danger\">Unpublished", index, StringComparison.Ordinal);
        Assert.Contains("item.IsPublished", index, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowOnFaqPage", index, StringComparison.Ordinal);
        Assert.Contains("Unpublish", index, StringComparison.Ordinal);
        Assert.Contains("Publish", index, StringComparison.Ordinal);
        Assert.Contains("<th>Status</th>", index, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, "badge bg-success\">Published", "Unpublish")]
    [InlineData(false, "badge bg-danger\">Unpublished", "Publish")]
    public void Faq_index_status_badge_matches_IsPublished(bool isPublished, string badgeMarkup, string actionLabel)
    {
        var root = FindRepoRoot();
        var index = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminFaqs", "Index.cshtml"));
        Assert.Contains("@if (item.IsPublished)", index, StringComparison.Ordinal);
        Assert.Contains(badgeMarkup, index, StringComparison.Ordinal);
        Assert.Contains(actionLabel, index, StringComparison.Ordinal);
        Assert.Contains(
            isPublished
                ? "btn-outline-danger"
                : "btn-outline-success",
            index,
            StringComparison.Ordinal);
    }

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

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "FinalProject.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);
}
