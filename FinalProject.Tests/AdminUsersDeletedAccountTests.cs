using FinalProject.MVC.Repositories.Implementations;
using System.Security.Claims;
using FinalProject.MVC.Controllers;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinalProject.Tests;

public class AdminUsersDeletedAccountTests
{
    [Fact]
    public void Users_view_separates_deleted_accounts_and_exposes_delete_action()
    {
        var view = LocateUsersViewContent();
        Assert.Contains("View deleted accounts", view, StringComparison.Ordinal);
        Assert.Contains("Back to active users", view, StringComparison.Ordinal);
        Assert.Contains("user.AccountStatus", view, StringComparison.Ordinal);
        Assert.Contains("user.IsDeleted", view, StringComparison.Ordinal);
        Assert.Contains("Role changes disabled", view, StringComparison.Ordinal);
        Assert.Contains("showDeleted", view, StringComparison.Ordinal);
        Assert.Contains("DeleteUser", view, StringComparison.Ordinal);
        Assert.Contains(">Delete<", view, StringComparison.Ordinal);
        Assert.NotNull(typeof(AdminController).GetMethod(nameof(AdminController.DeleteUser)));
    }

    [Fact]
    public async Task DeleteUser_soft_deletes_member_and_blocks_future_sign_in_flags()
    {
        await using var harness = await UsersHarness.CreateAsync();
        var member = await harness.CreateMemberAsync("remove-me@example.com");

        var result = await harness.Controller.DeleteUser(member.Id) as RedirectToActionResult;
        Assert.Equal(nameof(AdminController.Users), result!.ActionName);

        var deleted = await harness.Users.FindByIdAsync(member.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.True(deleted.LockoutEnabled);
        Assert.True(deleted.LockoutEnd.HasValue && deleted.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.True(SoftDeletedAccountIdentity.IsReleasedIdentifier(deleted.Email));
        Assert.True(SoftDeletedAccountIdentity.IsReleasedIdentifier(deleted.UserName));
        Assert.Null(await harness.Users.FindByEmailAsync("remove-me@example.com"));
        Assert.Contains("can no longer sign in", harness.Controller.TempData["Success"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("register again", harness.Controller.TempData["Success"]?.ToString(), StringComparison.OrdinalIgnoreCase);

        var activeList = Assert.IsType<AdminUsersPageViewModel>(
            Assert.IsType<ViewResult>(await harness.Controller.Users(null, showDeleted: false)).Model);
        Assert.DoesNotContain(activeList.Users, user => user.Id == member.Id);

        var deletedList = Assert.IsType<AdminUsersPageViewModel>(
            Assert.IsType<ViewResult>(await harness.Controller.Users(null, showDeleted: true)).Model);
        Assert.Contains(deletedList.Users, user => user.Id == member.Id && user.IsDeleted);
    }

    [Fact]
    public async Task DeleteUser_rejects_self_delete()
    {
        await using var harness = await UsersHarness.CreateAsync();
        var result = await harness.Controller.DeleteUser("super-admin-1") as RedirectToActionResult;
        Assert.Equal(nameof(AdminController.Users), result!.ActionName);
        Assert.Equal(
            "You cannot delete your own account from the admin panel.",
            harness.Controller.TempData["Error"]);
        Assert.False((await harness.Users.FindByIdAsync("super-admin-1"))!.IsDeleted);
    }

    [Fact]
    public async Task Active_users_list_excludes_soft_deleted_members()
    {
        await using var harness = await UsersHarness.CreateAsync();
        var active = await harness.CreateMemberAsync("active@example.com");
        var deleted = await harness.CreateMemberAsync("deleted@example.com");
        deleted.IsDeleted = true;
        deleted.DeletedAt = DateTime.UtcNow;
        deleted.LockoutEnabled = true;
        deleted.LockoutEnd = DateTimeOffset.MaxValue;
        await harness.Users.UpdateAsync(deleted);

        var result = await harness.Controller.Users(search: null, showDeleted: false) as ViewResult;
        var model = Assert.IsType<AdminUsersPageViewModel>(result!.Model);

        Assert.False(model.ShowDeleted);
        Assert.Contains(model.Users, user => user.Id == active.Id);
        Assert.DoesNotContain(model.Users, user => user.Id == deleted.Id);
        Assert.All(model.Users, user => Assert.False(user.IsDeleted));
    }

    [Fact]
    public async Task Deleted_users_list_shows_Deleted_status_not_Locked()
    {
        await using var harness = await UsersHarness.CreateAsync();
        var deleted = await harness.CreateMemberAsync("gone@example.com");
        deleted.IsDeleted = true;
        deleted.DeletedAt = DateTime.UtcNow.AddMinutes(-5);
        deleted.LockoutEnabled = true;
        deleted.LockoutEnd = DateTimeOffset.MaxValue;
        await harness.Users.UpdateAsync(deleted);

        var result = await harness.Controller.Users(search: null, showDeleted: true) as ViewResult;
        var model = Assert.IsType<AdminUsersPageViewModel>(result!.Model);

        Assert.True(model.ShowDeleted);
        var row = Assert.Single(model.Users, user => user.Id == deleted.Id);
        Assert.True(row.IsDeleted);
        Assert.Equal("Deleted", row.AccountStatus);
        Assert.NotEqual("Locked", row.AccountStatus);
    }

    [Fact]
    public async Task ChangeRole_rejects_soft_deleted_accounts()
    {
        await using var harness = await UsersHarness.CreateAsync();
        var deleted = await harness.CreateMemberAsync("frozen@example.com");
        deleted.IsDeleted = true;
        deleted.DeletedAt = DateTime.UtcNow;
        await harness.Users.UpdateAsync(deleted);

        var result = await harness.Controller.ChangeRole(deleted.Id, AppRoles.Admin) as RedirectToActionResult;
        Assert.Equal(nameof(AdminController.Users), result!.ActionName);
        Assert.Equal("Deleted accounts cannot have their roles changed.", harness.Controller.TempData["Error"]);

        var roles = await harness.Users.GetRolesAsync((await harness.Users.FindByIdAsync(deleted.Id))!);
        Assert.Contains(AppRoles.Member, roles);
        Assert.DoesNotContain(AppRoles.Admin, roles);
    }

    [Fact]
    public async Task ChangeRoles_batch_rejects_deleted_user_without_changing_role()
    {
        await using var harness = await UsersHarness.CreateAsync();
        var deleted = await harness.CreateMemberAsync("batch-deleted@example.com");
        deleted.IsDeleted = true;
        deleted.DeletedAt = DateTime.UtcNow;
        await harness.Users.UpdateAsync(deleted);

        await harness.Controller.ChangeRoles(
        [
            new AdminUserRoleChangeItem { UserId = deleted.Id, Role = AppRoles.Admin }
        ]);

        Assert.Equal(
            "Deleted accounts cannot have their roles changed.",
            harness.Controller.TempData["Error"]);
        Assert.Contains(AppRoles.Member, await harness.Users.GetRolesAsync(deleted));
    }

    private static string LocateUsersViewContent()
    {
        var relative = Path.Combine("FinalProject.MVC", "Views", "Admin", "Users.cshtml");
        foreach (var start in new[]
                 {
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory,
                     Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                     @"C:\Users\HP\OneDrive\Desktop\codeACA\codeACA\FinalProject"
                 })
        {
            var candidate = Path.Combine(start, relative);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                dir = dir.Parent;
            }
        }

        throw new InvalidOperationException("Could not locate Admin/Users.cshtml.");
    }

    private sealed class UsersHarness : IAsyncDisposable
    {
        private UsersHarness(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles,
            AdminController controller)
        {
            Db = db;
            Users = users;
            Roles = roles;
            Controller = controller;
        }

        public ApplicationDbContext Db { get; }
        public UserManager<ApplicationUser> Users { get; }
        public RoleManager<IdentityRole> Roles { get; }
        public AdminController Controller { get; }

        public static async Task<UsersHarness> CreateAsync()
        {
            var db = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("admin-users-deleted-" + Guid.NewGuid().ToString("N"))
                    .Options);

            var identityOptions = Options.Create(new IdentityOptions
            {
                Lockout =
                {
                    AllowedForNewUsers = true,
                    MaxFailedAccessAttempts = 5,
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)
                }
            });
            var users = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(db),
                identityOptions,
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                NullLogger<UserManager<ApplicationUser>>.Instance);

            var roles = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(db),
                Array.Empty<IRoleValidator<IdentityRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                NullLogger<RoleManager<IdentityRole>>.Instance);

            foreach (var role in AppRoles.All)
            {
                await roles.CreateAsync(new IdentityRole(role));
            }

            var superAdmin = new ApplicationUser
            {
                Id = "super-admin-1",
                UserName = "super@example.com",
                Email = "super@example.com",
                NormalizedEmail = "SUPER@EXAMPLE.COM",
                NormalizedUserName = "SUPER@EXAMPLE.COM",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            Assert.True((await users.CreateAsync(superAdmin)).Succeeded);
            await users.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);

            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, superAdmin.Id),
                        new Claim(ClaimTypes.Role, AppRoles.SuperAdmin)
                    ],
                    "test"))
            };

            var controller = new AdminController(
                users,
                tokenService: null!,
                adminApi: null!,
                cars: null!,
                dashboard: new AdminDashboardService(new ApplicationRepository(db), roles))
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                    http,
                    new NullTempDataProvider())
            };

            return new UsersHarness(db, users, roles, controller);
        }

        public async Task<ApplicationUser> CreateMemberAsync(string email)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = email,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            Assert.True((await Users.CreateAsync(user)).Succeeded);
            await Users.AddToRoleAsync(user, AppRoles.Member);
            return (await Users.FindByIdAsync(user.Id))!;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class NullTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
