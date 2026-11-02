using Xunit;

namespace FinalProject.Tests;

public class DevelopmentShadowRunTests
{
    [Fact]
    public void DevelopmentApiHost_starts_api_from_artifacts_run_shadow_not_dotnet_run()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var host = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Hosting", "DevelopmentApiHost.cs"));
        var runScript = File.ReadAllText(Path.Combine(root, "scripts", "dev-run.ps1"));
        var stopScript = File.ReadAllText(Path.Combine(root, "scripts", "dev-stop.ps1"));

        Assert.Contains("artifacts", host, StringComparison.Ordinal);
        Assert.Contains("run", host, StringComparison.Ordinal);
        Assert.Contains("api", host, StringComparison.Ordinal);
        Assert.Contains("SyncDirectory", host, StringComparison.Ordinal);
        Assert.Contains("FINALPROJECT_SHADOW_RUN", host, StringComparison.Ordinal);
        Assert.DoesNotContain("run --no-launch-profile --project", host, StringComparison.Ordinal);

        Assert.Contains("artifacts\\run", runScript, StringComparison.Ordinal);
        Assert.Contains("Join-Path $RunRoot \"api\"", runScript, StringComparison.Ordinal);
        Assert.Contains("Join-Path $RunRoot \"mvc\"", runScript, StringComparison.Ordinal);
        Assert.Contains("dev-run.pids.json", runScript, StringComparison.Ordinal);
        Assert.Contains("scripts/dev-run.ps1", stopScript, StringComparison.Ordinal);
        Assert.Contains("dev-run.pids.json", stopScript, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-Process dotnet", stopScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("taskkill /IM", stopScript, StringComparison.OrdinalIgnoreCase);
    }
}
