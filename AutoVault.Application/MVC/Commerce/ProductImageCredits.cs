namespace FinalProject.MVC.Commerce;

public sealed record ImageCredit(string Author, string License, string SourceUrl);

// Author, licence, and source for every Wikimedia Commons product photo. Not rendered on public product pages;
// CC BY and CC BY-SA still require attribution wherever the photos are published.
public static class ProductImageCredits
{
    private const string Folder = "~/assets/images/portfolio/parts/hd/";

    private static readonly Dictionary<string, ImageCredit[]> Credits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["engine-assembly.webp"] = [new("Mr.choppers", "CC BY-SA 3.0", "https://commons.wikimedia.org/wiki/File:1963_Impala_283_engine_underhood.jpg")],
        ["brake-caliper.webp"] = [new("Adam Russell", "CC BY-SA 2.0", "https://commons.wikimedia.org/wiki/File:Red_Hot_Brakes_(5893323749).jpg")],
        ["coilover-shock.webp"] = [new("Piero", "Public domain", "https://commons.wikimedia.org/wiki/File:Demper_ducati_01.jpg")],
        ["air-filter.webp"] = [new("Royalbroil", "CC BY-SA 2.5", "https://commons.wikimedia.org/wiki/File:K%26NAirFilterAndRechargerKit.jpg")],
        ["oil-filter.webp"] = [new("Liftarn", "CC BY-SA 3.0", "https://commons.wikimedia.org/wiki/File:Spin-on-conversion.jpg")],
        ["timing-belt.webp"] = [new("Terabass", "CC BY-SA 3.0", "https://commons.wikimedia.org/wiki/File:2001_honda_accord_timing_belt-terabass.jpg")],
        ["ball-bearing.webp"] = [new("R. Henrik Nilsson", "CC BY 4.0", "https://commons.wikimedia.org/wiki/File:1970_or_earlier_ball_bearing_330_V4_MTY_Hoffmann_Manufacturing_Company_England.jpg")],
        ["bore-gauge.webp"] = [new("Glenn McKechnie", "CC BY-SA 2.0", "https://commons.wikimedia.org/wiki/File:MicrometerInside5-30_25-50.jpg")],
        ["check-ring.webp"] = [new("Christoph Baumgartinger", "CC BY-SA 2.5", "https://commons.wikimedia.org/wiki/File:Not_Go_Ring.JPG")],
        ["turbocharger.webp"] = [new("Micool K", "CC BY-SA 4.0", "https://commons.wikimedia.org/wiki/File:Turbo_Mini_GT_(cropped).jpg")],
        ["exhaust-muffler.webp"] = [new("CrowzRSA", "CC BY-SA 4.0", "https://commons.wikimedia.org/wiki/File:CRX_OEM_Muffler_c.JPG")],
        ["exhaust-tip.webp"] = [new("Bindydad123", "CC BY-SA 4.0", "https://commons.wikimedia.org/wiki/File:2019_Mercedes-Benz_E_300_Exclusive_Line_(44).jpg")],
        ["splined-shaft.webp"] = [new("Hugo4", "CC BY-SA 3.0", "https://commons.wikimedia.org/wiki/File:Keilprofil-6K.JPG")],
        ["gear-shaft-fastener-kit.webp"] = [new("A7N8X", "CC BY-SA 4.0", "https://commons.wikimedia.org/wiki/File:Cambio_innesti_frontali.jpg")],
        ["outside-micrometer.webp"] = [new("Nathanael Burton", "CC BY-SA 2.0", "https://commons.wikimedia.org/wiki/File:Micrometer_Caliper.jpg")],
    };

    // Extra photos shown after the product's main image, one file per photo. Each must show the listed product itself.
    private static readonly Dictionary<string, string[]> ExtraPhotos = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Gallery(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return [];
        }

        if (!imagePath.StartsWith(Folder, StringComparison.OrdinalIgnoreCase)
            || !ExtraPhotos.TryGetValue(imagePath[Folder.Length..], out var extras))
        {
            return [imagePath];
        }

        return [imagePath, .. extras.Select(file => Folder + file)];
    }

    public static IReadOnlyList<ImageCredit> For(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith(Folder, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return Credits.TryGetValue(imagePath[Folder.Length..], out var credits) ? credits : [];
    }
}
