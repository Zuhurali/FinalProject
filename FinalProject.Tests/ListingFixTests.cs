using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace FinalProject.Tests;

public class ListingFixTests
{
    [Fact]
    public async Task Team_create_stays_on_the_form_and_does_not_claim_success_when_invalid()
    {
        var controller = CreateController(new RecordingTeams(ok: true));
        controller.ModelState.AddModelError(nameof(TeamMemberEditViewModel.Name), "The Name field is required.");

        var result = await controller.Create(new TeamMemberEditViewModel(), null, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Null(controller.TempData["Success"]);
    }

    [Fact]
    public async Task Team_create_redirects_to_the_list_only_after_a_successful_save()
    {
        var teams = new RecordingTeams(ok: true);
        var controller = CreateController(teams);

        var result = await controller.Create(Member(), null, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminTeamsController.Index), redirect.ActionName);
        Assert.Equal("Team member created successfully.", controller.TempData["Success"]);
        Assert.Equal(1, teams.Creates);
    }

    [Fact]
    public async Task Team_create_does_not_claim_success_when_the_save_fails()
    {
        var controller = CreateController(new RecordingTeams(ok: false, error: "Choose a team photo."));

        var result = await controller.Create(Member(), null, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Null(controller.TempData["Success"]);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Team_member_is_saved_and_a_missing_photo_does_not_insert_a_row()
    {
        await using var db = CreateDb();
        var service = new TeamMemberService(db, new TestEnvironment());
        var missingPhoto = Member();
        missingPhoto.ImagePath = "";

        var (failed, error, failedId) = await service.CreateAsync(missingPhoto, null);
        Assert.False(failed);
        Assert.Equal("Choose a team photo.", error);
        Assert.Null(failedId);
        Assert.Equal(0, await db.TeamMembers.CountAsync());

        var created = Member();
        created.IsPublished = true;
        var (ok, createError, id) = await service.CreateAsync(created, null);
        Assert.True(ok, createError);
        Assert.NotNull(id);

        var hidden = Member();
        hidden.Name = "Hidden Member";
        hidden.Email = "hidden@example.com";
        hidden.IsPublished = false;
        Assert.True((await service.CreateAsync(hidden, null)).Ok);

        var published = await service.GetPublishedAsync();
        Assert.Equal(new[] { "Ada Tester" }, published.Select(member => member.Name).ToArray());
        Assert.Equal("/uploads/team/ada.webp", published[0].ImagePath);
    }

    [Fact]
    public async Task Team_create_does_not_throw_when_the_image_path_is_null()
    {
        await using var db = CreateDb();
        var service = new TeamMemberService(db, new TestEnvironment());
        var missing = Member();
        missing.ImagePath = null!;

        var (failed, error, failedId) = await service.CreateAsync(missing, null);

        Assert.False(failed);
        Assert.Equal("Choose a team photo.", error);
        Assert.Null(failedId);
        Assert.Equal(0, await db.TeamMembers.CountAsync());

        IFormFile file = new StubUpload("notes.gif", "image/gif");
        var invalid = Member();
        invalid.ImagePath = null!;
        var (rejected, rejectedError, rejectedId) = await service.CreateAsync(invalid, file);
        Assert.False(rejected);
        Assert.Equal("Use an SVG, PNG, JPG, or WEBP image.", rejectedError);
        Assert.Null(rejectedId);
        Assert.Equal(0, await db.TeamMembers.CountAsync());
    }

    [Fact]
    public void Team_page_reveals_members_after_the_first_four_without_duplicating_markup()
    {
        var view = Read("FinalProject.MVC", "Views", "Team", "Index.cshtml");
        Assert.Contains("memberIndex >= 4", view, StringComparison.Ordinal);
        Assert.Contains("id=\"teamLoadMore\"", view, StringComparison.Ordinal);
        Assert.Contains("Load More", view, StringComparison.Ordinal);
        Assert.Contains("team-member-more", view, StringComparison.Ordinal);
        Assert.Contains("Model.Count > 4", view, StringComparison.Ordinal);
        Assert.Contains("id=\"teamMemberGrid\"", view, StringComparison.Ordinal);
        Assert.Contains("#teamMemberGrid .team-member-more", view, StringComparison.Ordinal);
    }

    [Fact]
    public void Browse_used_cars_uses_the_most_viewed_layout_with_a_used_filter()
    {
        var shop = Read("FinalProject.MVC", "Views", "Shop", "Index.cshtml");
        Assert.Contains("asp-action=\"MostViewed\"", shop, StringComparison.Ordinal);
        Assert.Contains("asp-route-condition=\"used\"", shop, StringComparison.Ordinal);

        var controller = Read("FinalProject.MVC", "Controllers", "CarsController.cs");
        Assert.Contains("usedOnly ? CarExplorePage.UsedPageSize : CarExplorePage.PageSize", controller, StringComparison.Ordinal);
        Assert.Contains("GetMostViewedAsync", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void CarsTwo_models_come_from_the_selected_brand_and_CarsOne_is_unchanged()
    {
        var carsTwo = Read("FinalProject.MVC", "Views", "Cars", "CarsTwo.cshtml");
        var carsOne = Read("FinalProject.MVC", "Views", "Cars", "CarsOne.cshtml");
        var controller = Read("FinalProject.MVC", "Controllers", "CarsController.cs");

        Assert.Contains("Select a brand first", carsTwo, StringComparison.Ordinal);
        Assert.Contains("data-brand-id", carsTwo, StringComparison.Ordinal);
        Assert.Contains("BrandModels", carsTwo, StringComparison.Ordinal);
        Assert.Contains("public async Task<IActionResult> BrandModels(int brandId)", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("BrandModels", carsOne, StringComparison.Ordinal);
        Assert.DoesNotContain("data-brand-id", carsOne, StringComparison.Ordinal);
    }

    [Fact]
    public void Favorite_heart_button_has_no_square_border_and_CarsThree_uses_persisted_seats()
    {
        var css = Read("FinalProject.MVC", "wwwroot", "assets", "css", "style.css");
        var explore = Read("FinalProject.MVC", "Views", "Cars", "Explore.cshtml");
        var mapper = Read("FinalProject.MVC", "Mappings", "CarApiMapper.cs");

        Assert.Contains(".project-wrapper2 .image-area form.wishlist button", css, StringComparison.Ordinal);
        Assert.Contains("border: 0;", explore, StringComparison.Ordinal);
        Assert.Contains("data-favorite-toggle", explore, StringComparison.Ordinal);
        Assert.Contains("ResolveSeats(dto.Seats)", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("SeatsToken", mapper, StringComparison.Ordinal);
    }

    private static AdminTeamsController CreateController(ITeamMemberService teams)
    {
        var controller = new AdminTeamsController(teams)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new DictionaryTempDataProvider());
        return controller;
    }

    private sealed class StubUpload : IFormFile
    {
        public StubUpload(string fileName, string contentType)
        {
            FileName = fileName;
            ContentType = contentType;
        }

        public string ContentType { get; }
        public string ContentDisposition => string.Empty;
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length => 3;
        public string Name => "image";
        public string FileName { get; }
        public void CopyTo(Stream target) { }
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Stream OpenReadStream() => new MemoryStream(new byte[] { 1, 2, 3 });
    }

    private static TeamMemberEditViewModel Member() => new()
    {
        Name = "Ada Tester",
        Position = "Advisor",
        Department = "Sales",
        Experience = "5 years",
        Biography = "Helps buyers compare cars.",
        Phone = "+994501112233",
        Email = "ada@example.com",
        SortOrder = 10,
        IsPublished = true,
        ImagePath = "/uploads/team/ada.webp"
    };

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("team-create-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string Read(params string[] parts)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FinalProject.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the solution root.");
    }

    private sealed class RecordingTeams : ITeamMemberService
    {
        private readonly bool _ok;
        private readonly string? _error;

        public RecordingTeams(bool ok, string? error = null)
        {
            _ok = ok;
            _error = error;
        }

        public int Creates { get; private set; }

        public Task<(bool Ok, string? Error, int? Id)> CreateAsync(
            TeamMemberEditViewModel model,
            IFormFile? image,
            CancellationToken cancellationToken = default)
        {
            Creates++;
            return Task.FromResult<(bool, string?, int?)>(_ok ? (true, null, 1) : (false, _error, null));
        }

        public Task<IReadOnlyList<TeamMember>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TeamMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TeamMember?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task EnsureSeedAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Ok, string? Error)> UpdateAsync(
            TeamMemberEditViewModel model,
            IFormFile? image,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Ok, string? Error)> MoveAsync(int id, int direction, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class DictionaryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "FinalProject.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
