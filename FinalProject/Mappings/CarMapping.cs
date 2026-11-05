using FinalProject.Commerce;
using FinalProject.DTOs;
using FinalProject.Models.Entities;

namespace FinalProject.Mappings;

public static class CarMapping
{
    public static CarResponseDto ToResponse(this Car car)
    {
        return new CarResponseDto
        {
            Id = car.Id,
            Model = car.Model,
            Year = car.Year,
            Price = car.Price,
            Mileage = car.Mileage,
            FuelType = car.FuelType,
            Transmission = car.Transmission,
            Color = car.Color,
            Seats = car.Seats,
            DriveType = car.DriveType,
            Engine = car.Engine,
            MileageUnit = car.MileageUnit,
            Horsepower = car.Horsepower,
            Doors = car.Doors,
            Cylinders = car.Cylinders,
            Steering = car.Steering,
            Vin = car.Vin,
            AccidentHistory = car.AccidentHistory,
            RepaintedParts = car.RepaintedParts,
            ReplacedParts = car.ReplacedParts,
            CustomsStatus = car.CustomsStatus,
            MarketOrigin = car.MarketOrigin,
            Features = CarFeatureCatalog.ReadKeys(car.Features).ToList(),
            Description = car.Description,
            IsNew = car.IsNew,
            IsFeatured = car.IsFeatured,
            OwnerId = car.OwnerId,
            OwnerDisplayName = car.Owner == null
                ? null
                : DisplayName(car.Owner),
            Status = car.Status,
            CreatedAt = car.CreatedAt,
            UpdatedAt = car.UpdatedAt,
            Brand = new BrandResponseDto
            {
                Id = car.Brand.Id,
                Name = car.Brand.Name,
                CreatedAt = car.Brand.CreatedAt
            },
            Category = new CategoryResponseDto
            {
                Id = car.Category.Id,
                Name = car.Category.Name,
                IconPath = car.Category.IconPath ?? string.Empty
            },
            Images = car.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => new CarImageResponseDto
                {
                    Id = image.Id,
                    ImageUrl = image.ImageUrl,
                    IsMain = image.IsMain,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList()
        };
    }

    private static string DisplayName(IdentityUserReference user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? user.Email ?? user.UserName ?? "User"
            : fullName;
    }
}
