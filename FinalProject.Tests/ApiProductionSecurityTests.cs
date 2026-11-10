using System.Reflection;
using FinalProject.Controllers;
using FinalProject.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace FinalProject.Tests;

public class ApiProductionSecurityTests
{
    [Fact]
    public void Api_Program_enables_production_exception_handling_and_hsts_like_mvc()
    {
        var apiProgram = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject", "Program.cs"));
        var mvcProgram = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject.MVC", "Program.cs"));

        Assert.Contains("UseExceptionHandler", mvcProgram, StringComparison.Ordinal);
        Assert.Contains("UseHsts", mvcProgram, StringComparison.Ordinal);
        Assert.Contains("!app.Environment.IsDevelopment()", mvcProgram, StringComparison.Ordinal);

        Assert.Contains("else", apiProgram, StringComparison.Ordinal);
        Assert.Contains("UseExceptionHandler", apiProgram, StringComparison.Ordinal);
        Assert.Contains("UseHsts", apiProgram, StringComparison.Ordinal);
        Assert.Contains("An unexpected error occurred.", apiProgram, StringComparison.Ordinal);
        Assert.Contains("application/json", apiProgram, StringComparison.Ordinal);

        // Swagger stays Development-only; JWT and HTTPS redirection remain.
        Assert.Contains("IsDevelopment()", apiProgram, StringComparison.Ordinal);
        Assert.Contains("UseSwagger", apiProgram, StringComparison.Ordinal);
        Assert.Contains("IPAddress.IsLoopback", apiProgram, StringComparison.Ordinal);
        Assert.Contains("localhost", apiProgram, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(apiProgram, "UseSwagger("));
        Assert.Contains("UseHttpsRedirection", apiProgram, StringComparison.Ordinal);
        Assert.Contains("AddJwtBearer", apiProgram, StringComparison.Ordinal);
        Assert.Contains("SwaggerAuthorizeOperationFilter", apiProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSecurityRequirement", apiProgram, StringComparison.Ordinal);
        Assert.Contains(
            "return BadRequest(new { message = \"Car id must be greater than zero.\" });",
            File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject", "Controllers", "CarsController.cs")),
            StringComparison.Ordinal);
        Assert.DoesNotContain("UseCors", apiProgram, StringComparison.Ordinal);

        var swaggerIndex = apiProgram.IndexOf("UseSwagger", StringComparison.Ordinal);
        var elseIndex = apiProgram.IndexOf("else", swaggerIndex, StringComparison.Ordinal);
        var hstsIndex = apiProgram.IndexOf("UseHsts", StringComparison.Ordinal);
        Assert.True(swaggerIndex >= 0 && elseIndex > swaggerIndex && hstsIndex > elseIndex);
    }

    [Fact]
    public void Car_views_endpoint_is_anonymous_and_rate_limited()
    {
        var method = typeof(CarsController).GetMethod(nameof(CarsController.RecordView))!;
        var rateLimit = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(rateLimit);
        Assert.Equal(CarViewsRateLimit.PolicyName, rateLimit!.PolicyName);
        Assert.Null(method.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());

        var produces = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();
        Assert.Contains(produces, item => item.StatusCode == StatusCodes.Status429TooManyRequests);
        Assert.Contains(produces, item => item.StatusCode == StatusCodes.Status204NoContent);
    }

    [Fact]
    public void Api_Program_registers_car_views_rate_limiter_policy()
    {
        var apiProgram = File.ReadAllText(Path.Combine(FindRepoRoot(), "FinalProject", "Program.cs"));

        Assert.Contains("AddRateLimiter", apiProgram, StringComparison.Ordinal);
        Assert.Contains("UseRateLimiter", apiProgram, StringComparison.Ordinal);
        Assert.Contains("CarViewsRateLimit.PolicyName", apiProgram, StringComparison.Ordinal);
        Assert.Contains("Status429TooManyRequests", apiProgram, StringComparison.Ordinal);
        Assert.Contains("Too many view requests", apiProgram, StringComparison.Ordinal);
        Assert.True(CarViewsRateLimit.PermitLimitPerMinute >= 60);
        Assert.Equal("car-views", CarViewsRateLimit.PolicyName);
    }

    [Fact]
    public void Mvc_RecordViewAsync_still_posts_anonymous_views_path()
    {
        var carApi = File.ReadAllText(Path.Combine(FindRepoRoot(), "AutoVault.Service", "MVC", "Services", "CarApiService.cs"));

        Assert.Contains(
            "HttpMethod.Post, $\"api/cars/{id}/views\"",
            carApi,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Authorized(HttpMethod.Post, $\"api/cars/{id}/views\"", carApi, StringComparison.Ordinal);
    }

    private static int CountOf(string text, string value)
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
}
