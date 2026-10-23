using System.Text.RegularExpressions;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Mappings;

public static class CarApiMapper
{
    private static readonly Regex EngineText = new(
        @"^(\d+(\.\d+)?L\b.*|(single|dual|tri|quad)[- ]motor)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static Car ToCar(CarApiDto dto)
    {
        var brand = dto.Brand?.Name ?? string.Empty;
        var model = dto.Model ?? string.Empty;
        var description = dto.Description ?? string.Empty;
        var fallbackName = $"{brand} {model}".Trim();
        var name = string.IsNullOrWhiteSpace(description)
            ? fallbackName
            : description.Split(". ", 2, StringSplitOptions.None)[0];
        var seats = 0;
        var driveType = NormalizeDriveType(dto.DriveType);
        var engine = string.Empty;
        var descriptionIsSpecSummary = string.IsNullOrWhiteSpace(description)
            || string.Equals(description.Trim().TrimEnd('.'), name, StringComparison.Ordinal);
        var specIndex = description.IndexOf(". ", StringComparison.Ordinal);
        if (specIndex >= 0)
        {
            var specParts = description[(specIndex + 2)..]
                .Trim()
                .TrimEnd('.')
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (specParts.Length is > 0 and <= 3 && TryParseSeats(specParts[0], out var parsedSeats))
            {
                seats = parsedSeats;
                var recognised = 1;
                foreach (var part in specParts.Skip(1))
                {
                    var drive = NormalizeDriveType(part);
                    if (drive.Length > 0)
                    {
                        if (driveType.Length == 0)
                        {
                            driveType = drive;
                        }
                        recognised++;
                    }
                    else if (engine.Length == 0 && EngineText.IsMatch(part))
                    {
                        engine = char.ToUpperInvariant(part[0]) + part[1..];
                        recognised++;
                    }
                }

                descriptionIsSpecSummary = recognised == specParts.Length;
            }
        }

        return new Car
        {
            Id = dto.Id,
            Name = name,
            Make = brand,
            Model = model,
            Category = dto.Category?.Name ?? string.Empty,
            Price = dto.Price,
            Year = dto.Year,
            Mileage = dto.Mileage,
            Fuel = dto.FuelType,
            Transmission = dto.Transmission,
            Color = dto.Color,
            DriveType = driveType,
            Engine = engine,
            DescriptionIsSpecSummary = descriptionIsSpecSummary,
            Description = string.IsNullOrWhiteSpace(description) ? name : description,
            ImagePath = NormalizeImagePath(dto.MainImage),
            GalleryImages = (dto.Images ?? new List<CarImageApiDto>())
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .Select(image => NormalizeImagePath(image.ImageUrl))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ImageCount = dto.Images == null || dto.Images.Count == 0 ? 1 : dto.Images.Count,
            IsNew = dto.IsNew,
            IsFeatured = dto.IsFeatured,
            Badge = dto.IsNew ? "New" : "Used",
            Seats = seats,
            InStock = string.Equals(dto.Status, "Active", StringComparison.OrdinalIgnoreCase),
            OwnerId = dto.OwnerId,
            OwnerDisplayName = dto.OwnerDisplayName,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
            CreatedAt = dto.CreatedAt
        };
    }

    public static async Task<IReadOnlyList<Car>> LoadCarsAsync(ICarApiService carApi, CancellationToken cancellationToken = default)
    {
        var cars = await carApi.GetAllCarsAsync(cancellationToken: cancellationToken);
        return cars.Select(ToCar).ToList();
    }

    public static string CountLabel(IEnumerable<Car> cars, string categoryName)
    {
        var category = CarCategories.Resolve(categoryName);
        var count = category == null
            ? 0
            : cars.Count(car => car.Category.Equals(category.Value, StringComparison.OrdinalIgnoreCase));
        return count == 1 ? "1 Car" : $"{count} Cars";
    }

    private static bool TryParseSeats(string part, out int seats)
    {
        seats = 0;
        return part.EndsWith(" seats", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(part[..^" seats".Length], out seats)
            && seats > 0;
    }

    public static string NormalizeDriveType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();
        if (text.Contains("all-wheel", StringComparison.OrdinalIgnoreCase)
            || text.Equals("AWD", StringComparison.OrdinalIgnoreCase))
        {
            return "AWD";
        }

        if (text.Contains("four-wheel", StringComparison.OrdinalIgnoreCase)
            || text.Equals("4WD", StringComparison.OrdinalIgnoreCase))
        {
            return "4WD";
        }

        if (text.Contains("rear-wheel", StringComparison.OrdinalIgnoreCase)
            || text.Equals("RWD", StringComparison.OrdinalIgnoreCase))
        {
            return "RWD";
        }

        if (text.Contains("front-wheel", StringComparison.OrdinalIgnoreCase)
            || text.Equals("FWD", StringComparison.OrdinalIgnoreCase))
        {
            return "FWD";
        }

        return string.Empty;
    }

    public static string NormalizeImagePath(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        if (imageUrl.StartsWith("~/", StringComparison.Ordinal))
        {
            return imageUrl;
        }

        if (imageUrl.StartsWith('/'))
        {
            return "~" + imageUrl;
        }

        return imageUrl;
    }
}
