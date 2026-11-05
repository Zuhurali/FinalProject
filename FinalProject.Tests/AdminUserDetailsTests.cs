using System.Reflection;
using System.Security.Claims;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.DTOs;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinalProject.Tests;

public class AdminUserDetailsTests
{
    [Fact]
    public void UserDetails_and_cars_by_owner_require_SuperAdmin_only()
    {
        var userDetailsRoles = typeof(AdminController).GetMethod(nameof(AdminController.UserDetails))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Roles!
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(AppRoles.SuperAdmin, userDetailsRoles);
        Assert.DoesNotContain(AppRoles.Admin, userDetailsRoles);
        Assert.DoesNotContain(AppRoles.Member, userDetailsRoles);

        Assert.Equal(
            "SuperAdmin",
            typeof(FinalProject.Controllers.CarsController)
                .GetMethod(nameof(FinalProject.Controllers.CarsController.GetByOwner))!
                .GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    [Fact]
    public void Conversation_view_links_other_participant_only_for_SuperAdmin()
    {
        var viewPath = Path.Combine(
            FindRepoRoot(),
            "FinalProject.MVC",
            "Views",
            "Account",
            "Conversation.cshtml");
        var source = File.ReadAllText(viewPath);
        Assert.Contains("User.IsInRole(\"SuperAdmin\")", source);
        Assert.Contains("asp-controller=\"Admin\"", source);
        Assert.Contains("asp-action=\"UserDetails\"", source);
        Assert.Contains("asp-route-id=\"@Model.BuyerId\"", source);
        Assert.Contains("asp-route-id=\"@Model.SellerId\"", source);
        Assert.Contains("!string.Equals(currentUserId, Model.BuyerId", source);
        Assert.Contains("!string.Equals(currentUserId, Model.SellerId", source);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FinalProject.MVC", "Views", "Account", "Conversation.cshtml")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    [Fact]
    public async Task UserDetails_loads_profile_listings_and_scoped_requests()
    {
        await using var storeDb = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("user-details-store-" + Guid.NewGuid().ToString("N"))
                .Options);

        var userManager = new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser>(storeDb),
            null!,
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        var roleManager = new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(storeDb),
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);
        foreach (var role in AppRoles.All)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var superAdmin = new ApplicationUser
        {
            Id = "super-1",
            UserName = "super@example.com",
            Email = "super@example.com",
            NormalizedEmail = "SUPER@EXAMPLE.COM",
            NormalizedUserName = "SUPER@EXAMPLE.COM",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        Assert.True((await userManager.CreateAsync(superAdmin)).Succeeded);
        await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);

        var member = new ApplicationUser
        {
            Id = "member-1",
            UserName = "member@example.com",
            Email = "member@example.com",
            NormalizedEmail = "MEMBER@EXAMPLE.COM",
            NormalizedUserName = "MEMBER@EXAMPLE.COM",
            EmailConfirmed = true,
            FirstName = "Mem",
            LastName = "Ber",
            PhoneNumber = "555-0100",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            LastLoginAt = DateTime.UtcNow.AddHours(-1),
            SecurityStamp = Guid.NewGuid().ToString()
        };
        Assert.True((await userManager.CreateAsync(member)).Succeeded);
        await userManager.AddToRoleAsync(member, AppRoles.Member);

        storeDb.ShopProducts.Add(new ShopCatalogProduct
        {
            Id = 1,
            Name = "Oil Filter",
            Price = 10,
            Quantity = 5,
            Category = "Filters",
            Brand = "ACME",
            ImagePath = "/images/parts/oil-filter.jpg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        storeDb.ContactRequests.Add(new ContactRequest
        {
            CustomerName = "Mem Ber",
            Email = member.Email!,
            Phone = "555-0100",
            Interest = "Showroom visit",
            Message = "Please call me about a visit tomorrow.",
            Status = ContactRequestStatus.New,
            UserId = member.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        storeDb.ContactRequests.Add(new ContactRequest
        {
            CustomerName = "Other",
            Email = "other@example.com",
            Phone = "555-9999",
            Interest = "Other interest",
            Message = "Should not appear for this member account.",
            Status = ContactRequestStatus.New,
            UserId = "someone-else",
            CreatedAt = DateTime.UtcNow
        });
        storeDb.ShowroomPurchaseRequests.Add(new ShowroomPurchaseRequest
        {
            ProductId = 1,
            CustomerName = "Mem Ber",
            Email = member.Email!,
            Phone = "555-0100",
            Quantity = 1,
            SubmissionKey = Guid.NewGuid(),
            Status = ShowroomRequestStatus.New,
            UserId = member.Id,
            CreatedAt = DateTime.UtcNow.AddHours(-5)
        });
        await storeDb.SaveChangesAsync();

        var controller = new AdminController(
            userManager,
            new StubTokenService(),
            null!,
            new StubCarApiService(
            [
                new CarApiDto
                {
                    Id = 42,
                    Model = "Model S",
                    Year = 2022,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Brand = new BrandApiDto { Id = 1, Name = "Tesla" },
                    OwnerId = member.Id
                }
            ]),
            storeDb,
            roleManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, superAdmin.Id),
                            new Claim(ClaimTypes.Role, AppRoles.SuperAdmin)
                        ],
                        "test"))
                }
            }
        };

        var result = await controller.UserDetails(member.Id, CancellationToken.None) as ViewResult;
        var model = Assert.IsType<AdminUserDetailsViewModel>(result!.Model);
        Assert.Equal("Mem", model.FirstName);
        Assert.Equal("Ber", model.LastName);
        Assert.Equal(member.Email, model.Email);
        Assert.Equal("555-0100", model.PhoneNumber);
        Assert.True(model.EmailConfirmed);
        Assert.False(model.PhoneNumberConfirmed);
        Assert.Equal(AppRoles.Member, model.Role);
        Assert.NotNull(model.LastLoginAt);
        Assert.False(model.ListingsUnavailable);
        Assert.Single(model.Listings);
        Assert.Equal("Tesla Model S (2022)", model.Listings[0].Title);
        Assert.Equal("Active", model.Listings[0].Status);
        Assert.Single(model.ContactRequests);
        Assert.Equal("Showroom visit", model.ContactRequests[0].Summary);
        Assert.Single(model.PurchaseRequests);
        Assert.Equal("Oil Filter", model.PurchaseRequests[0].Summary);
    }

    private sealed class StubTokenService : IApiAccessTokenService
    {
        public string CreateToken(ClaimsPrincipal user, IEnumerable<string> roles) => "test-token";
    }

    private sealed class StubCarApiService : ICarApiService
    {
        private readonly IReadOnlyList<CarApiDto> _cars;

        public StubCarApiService(IReadOnlyList<CarApiDto> cars) => _cars = cars;

        public Task<IReadOnlyList<CarApiDto>> GetCarsByOwnerAsync(
            string ownerId,
            string accessToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CarApiDto>>(
                _cars.Where(car => car.OwnerId == ownerId).ToList());

        public Task<PagedCarsDto> GetCarsAsync(CarApiQueryDto query, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CarApiDto>> GetAllCarsAsync(CarApiQueryDto? query = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<CarApiDto?> GetCarByIdAsync(int id, string? accessToken = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CarSuggestionDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<CarLookupDto> GetLookupsAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<CarApiDto>> GetMyCarsAsync(string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<CarApiDto> CreateCarAsync(SaveCarDto car, string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<CarUpdateResultDto> UpdateCarAsync(int id, SaveCarDto car, string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task DeleteCarAsync(int id, string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<CarApiDto> CreateCatalogCarAsync(SaveCarDto car, string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task RecordViewAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<PagedRankedCarsDto> GetMostViewedAsync(string period, int page = 1, int pageSize = 9, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<PagedRankedCarsDto> GetMostFavoritedAsync(int page = 1, int pageSize = 9, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<PagedCarsDto> GetLatestArrivalsAsync(int page = 1, int pageSize = 9, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<CarDiscoveryStatisticsDto> GetDiscoveryStatisticsAsync(string accessToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
