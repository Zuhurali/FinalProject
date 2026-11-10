using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FinalProject.Tests;

[Collection("CampaignSql")]
public class AdminDashboardHttpTests
{
    private const string BaseUrl = "https://localhost:7255";
    private const string Email = "campaigne2e@autovault.local";
    private const string Password = "CampaignE2E-2026!";

    [Fact]
    public async Task Anonymous_visitor_is_redirected_from_admin_dashboard()
    {
        if (!await MvcIsReachableAsync())
        {
            return;
        }

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(20) };
        var response = await client.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/SignIn", response.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_dashboard_renders_live_sql_backed_counts()
    {
        if (!await MvcIsReachableAsync())
        {
            return;
        }

        await EnsureAdminAsync();

        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
        var expectedContacts = await db.ContactRequests.CountAsync(request => request.Status == ContactRequestStatus.New);
        var expectedParts = await db.ShopProducts.CountAsync();

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };

        var signInPage = await client.GetAsync("/Account/SignIn?ReturnUrl=%2FAdmin");
        var signInHtml = await signInPage.Content.ReadAsStringAsync();
        var loginToken = Antiforgery(signInHtml);
        Assert.False(string.IsNullOrWhiteSpace(loginToken));

        var login = await client.PostAsync("/Account/SignIn", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["identifier"] = Email,
            ["password"] = Password,
            ["rememberMe"] = "false",
            ["returnUrl"] = "/Admin",
            ["__RequestVerificationToken"] = loginToken!
        }));
        Assert.True(login.IsSuccessStatusCode || (int)login.StatusCode is >= 300 and < 400);

        var dashboard = await client.GetAsync("/Admin");
        var html = await dashboard.Content.ReadAsStringAsync();
        Assert.True(dashboard.IsSuccessStatusCode, $"Dashboard status {(int)dashboard.StatusCode}");
        Assert.Contains("Dashboard", html, StringComparison.Ordinal);
        Assert.Contains("Needs attention", html, StringComparison.Ordinal);
        Assert.Contains("Spare Parts", html, StringComparison.Ordinal);
        Assert.Contains("New contact requests", html, StringComparison.Ordinal);
        Assert.Contains(expectedContacts.ToString(), html, StringComparison.Ordinal);
        Assert.Contains(expectedParts.ToString(), html, StringComparison.Ordinal);
        Assert.DoesNotContain("Total Products", html, StringComparison.Ordinal);
    }

    private static async Task<bool> MvcIsReachableAsync()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync("/Account/SignIn");
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect;
        }
        catch
        {
            return false;
        }
    }

    private static string? Antiforgery(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task EnsureAdminAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                "Server=VICTUS\\SQLEXPRESS;Database=FinalProjectDb_Test;Trusted_Connection=True;TrustServerCertificate=True"));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        var user = await users.FindByEmailAsync(Email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var created = await users.CreateAsync(user, Password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        }
        else
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var reset = await users.ResetPasswordAsync(user, token, Password);
            Assert.True(reset.Succeeded, string.Join(", ", reset.Errors.Select(error => error.Description)));
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await users.UpdateAsync(user);
            }
        }

        var current = await users.GetRolesAsync(user);
        foreach (var role in current.Where(role => role != AppRoles.Admin && role != AppRoles.SuperAdmin))
        {
            await users.RemoveFromRoleAsync(user, role);
        }

        if (!await users.IsInRoleAsync(user, AppRoles.Admin)
            && !await users.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            await users.AddToRoleAsync(user, AppRoles.Admin);
        }
    }
}
