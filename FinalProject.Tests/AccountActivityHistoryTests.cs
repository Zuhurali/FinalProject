using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services;
using Xunit;

namespace FinalProject.Tests;

public class AccountActivityHistoryTests
{
    [Fact]
    public void BuildKey_is_stable_for_same_inputs()
    {
        var createdAt = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        var first = AccountActivityHistory.BuildKey("message", "/Account/Messages/3", createdAt);
        var second = AccountActivityHistory.BuildKey("message", "/Account/Messages/3", createdAt);
        Assert.Equal(first, second);
        Assert.Contains("message|", first);
    }

    [Fact]
    public void ResolveKey_prefers_server_key()
    {
        var activity = new AccountActivityDto
        {
            Key = "server-key",
            Type = "listing",
            Url = "/Cars/Details/1",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("server-key", AccountActivityHistory.ResolveKey(activity));
    }
}
