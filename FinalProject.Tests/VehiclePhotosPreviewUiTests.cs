using Microsoft.Playwright;
using Xunit;

namespace FinalProject.Tests;

public class VehiclePhotosPreviewUiTests
{
    private const string BaseUrl = "https://localhost:7245";
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void Vehicle_photos_picker_script_processes_every_file_and_appends()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "wwwroot", "js", "vehicle-photos-picker.js"));
        var account = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "Account", "_VehicleListingFields.cshtml"));
        var admin = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarFields.cshtml"));
        var pickerPartial = File.ReadAllText(Path.Combine(root, "FinalProject.MVC", "Views", "AdminCars", "_CarImagePickerScripts.cshtml"));

        Assert.Contains("vehicle-photos-picker.js", pickerPartial, StringComparison.Ordinal);
        Assert.Contains("for (var i = 0; i < fileList.length; i++)", script, StringComparison.Ordinal);
        Assert.Contains("selectedFiles.push(file)", script, StringComparison.Ordinal);
        Assert.Contains("photoInput.multiple = true", script, StringComparison.Ordinal);
        Assert.DoesNotContain("selectedFiles = Array.prototype.slice.call(photoInput.files", script, StringComparison.Ordinal);
        Assert.Contains("multiple=\"multiple\"", account, StringComparison.Ordinal);
        Assert.Contains("multiple=\"multiple\"", admin, StringComparison.Ordinal);
        Assert.Contains("id=\"vehicle-photos-preview\"", account, StringComparison.Ordinal);
        Assert.Contains("id=\"vehicle-photos-preview\"", admin, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(auto-fill, minmax(140px, 160px))", admin, StringComparison.Ordinal);
        Assert.Contains("gap: 12px", admin, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Selecting_three_actual_image_files_renders_three_side_by_side_preview_cards()
    {
        if (!await IsMvcReachableAsync())
        {
            // Same soft-skip style as other localhost HTTP tests when the app is down.
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "av-photos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var imagePaths = new List<string>();
        for (var i = 1; i <= 3; i++)
        {
            var path = Path.Combine(tempDir, $"car-photo-{i}.png");
            await File.WriteAllBytesAsync(path, TinyPng);
            imagePaths.Add(path);
        }

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true
            });

            await page.GotoAsync(BaseUrl + "/js/vehicle-photos-picker.fixture.html", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await page.SetInputFilesAsync("#vehicle-photos", imagePaths);

            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('#vehicle-photos-preview .av-vehicle-photo-card').length === 3");

            var metrics = await page.EvaluateAsync<PreviewMetrics>(@"() => {
                const preview = document.getElementById('vehicle-photos-preview');
                const cards = [...preview.querySelectorAll('.av-vehicle-photo-card')];
                const rects = cards.map(c => {
                    const r = c.getBoundingClientRect();
                    return { x: r.left, y: r.top, w: r.width, h: r.height };
                });
                const sideBySide = cards.length >= 2
                    && Math.abs(rects[0].y - rects[1].y) < 12
                    && rects[1].x > rects[0].x + 20;
                return {
                    cardCount: cards.length,
                    fileCount: window.__avVehiclePhotosPicker.getCount(),
                    sideBySide,
                    names: cards.map(c => c.getAttribute('data-file-name')),
                    display: getComputedStyle(preview).display,
                    gridTemplateColumns: getComputedStyle(preview).gridTemplateColumns
                };
            }");

            Assert.Equal(3, metrics.CardCount);
            Assert.Equal(3, metrics.FileCount);
            Assert.True(metrics.SideBySide, "Preview cards must sit side by side in the grid, not stacked as a single column.");
            Assert.Equal("grid", metrics.Display);
            Assert.Contains("car-photo-1.png", metrics.Names!);
            Assert.Contains("car-photo-2.png", metrics.Names!);
            Assert.Contains("car-photo-3.png", metrics.Names!);

            // Sequential picks must append, not replace (the bug that left users with one card).
            await page.EvaluateAsync("() => window.__avVehiclePhotosPicker.clear()");
            await page.SetInputFilesAsync("#vehicle-photos", new[] { imagePaths[0] });
            await page.SetInputFilesAsync("#vehicle-photos", new[] { imagePaths[1] });
            await page.SetInputFilesAsync("#vehicle-photos", new[] { imagePaths[2] });
            var sequential = await page.EvaluateAsync<int>(
                "() => document.querySelectorAll('#vehicle-photos-preview .av-vehicle-photo-card').length");
            Assert.Equal(3, sequential);
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Browser download", StringComparison.OrdinalIgnoreCase))
        {
            // Chromium not installed for Playwright yet — install once then fail clearly if still missing.
            Microsoft.Playwright.Program.Main(["install", "chromium"]);
            Assert.Fail("Playwright Chromium was missing; it has been installed. Re-run the test suite.");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore */ }
        }
    }

    private sealed class PreviewMetrics
    {
        public int CardCount { get; set; }
        public int FileCount { get; set; }
        public bool SideBySide { get; set; }
        public string? Display { get; set; }
        public string? GridTemplateColumns { get; set; }
        public string[]? Names { get; set; }
    }

    private static async Task<bool> IsMvcReachableAsync()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync(BaseUrl + "/js/vehicle-photos-picker.js");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinalProject.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "FinalProject.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
