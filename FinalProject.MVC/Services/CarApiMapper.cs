using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Models.Api;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public static class CarApiMapper
{
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
        var driveType = dto.DriveType ?? string.Empty;
        var specIndex = description.IndexOf(". ", StringComparison.Ordinal);
        if (specIndex >= 0)
        {
            var spec = description[(specIndex + 2)..].Trim().TrimEnd('.');
            var seatSplit = spec.Split(" seats, ", 2, StringSplitOptions.None);
            if (seatSplit.Length == 2 && int.TryParse(seatSplit[0], out var parsedSeats))
            {
                seats = parsedSeats;
                if (string.IsNullOrWhiteSpace(driveType))
                {
                    driveType = seatSplit[1];
                }
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
            Description = string.IsNullOrWhiteSpace(description) ? name : description,
            ImagePath = NormalizeImagePath(dto.MainImage),
            ImageCount = dto.Images == null || dto.Images.Count == 0 ? 1 : dto.Images.Count,
            Badge = dto.IsNew ? "New" : string.Empty,
            Seats = seats,
            InStock = dto.Status == "Active",
            OwnerId = dto.OwnerId,
            OwnerDisplayName = dto.OwnerDisplayName,
            Status = dto.Status,
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
