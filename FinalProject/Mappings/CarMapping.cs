using FinalProject.DTOs;
using FinalProject.Models.Entities;

namespace FinalProject.Mappings;

public static class CarMapping
{
    public static CarResponse ToResponse(this Car car)
    {
        return new CarResponse
        {
            Id = car.Id,
            Model = car.Model,
            Year = car.Year,
            Price = car.Price,
            Mileage = car.Mileage,
            FuelType = car.FuelType,
            Transmission = car.Transmission,
            Color = car.Color,
            Description = car.Description,
            IsNew = car.IsNew,
            IsFeatured = car.IsFeatured,
            CreatedAt = car.CreatedAt,
            UpdatedAt = car.UpdatedAt,
            Brand = new BrandResponse
            {
                Id = car.Brand.Id,
                Name = car.Brand.Name,
                CreatedAt = car.Brand.CreatedAt
            },
            Category = new CategoryResponse
            {
                Id = car.Category.Id,
                Name = car.Category.Name
            },
            Images = car.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => new CarImageResponse
                {
                    Id = image.Id,
                    ImageUrl = image.ImageUrl,
                    IsMain = image.IsMain,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList()
        };
    }
}
