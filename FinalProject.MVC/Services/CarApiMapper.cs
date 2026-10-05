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
        var name = string.IsNullOrWhiteSpace(dto.Description)
            ? $"{brand} {model}".Trim()
            : dto.Description;

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
            DriveType = dto.DriveType ?? string.Empty,
            Description = name,
            ImagePath = NormalizeImagePath(dto.MainImage),
            ImageCount = dto.Images == null || dto.Images.Count == 0 ? 1 : dto.Images.Count,
            Badge = dto.IsNew ? "New" : string.Empty,
            Seats = 0,
            InStock = true
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
