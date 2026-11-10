using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Models.Entities;
using FinalProject.Repositories;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _cars;
    private readonly INotificationService _notifications;

    public CarService(ICarRepository cars, INotificationService notifications)
    {
        _cars = cars;
        _notifications = notifications;
    }

    public async Task<PagedResultDto<CarResponseDto>> QueryAsync(CarListQueryDto query, CancellationToken cancellationToken = default)
    {
        var criteria = ToCriteria(query);
        var totalCount = await _cars.CountCarsMatchingAsync(criteria, cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        IReadOnlyList<CarResponseDto> items = Array.Empty<CarResponseDto>();
        if (totalCount > 0 && query.Page <= totalPages)
        {
            var cars = await _cars.GetCarsMatchingPageAsync(criteria, query.Page, query.PageSize, cancellationToken);
            items = cars.Select(car => car.ToResponse()).ToList();
        }

        return new PagedResultDto<CarResponseDto>
        {
            Items = items,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<IReadOnlyList<CarSuggestionResponseDto>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var term = query.Trim().ToLower();
        var brandNames = await _cars.GetBrandNamesContainingAsync(term, cancellationToken);

        var carNames = await _cars.GetActiveCarDescriptionsMatchingAsync(
            CarPublicListing.ActiveStatus,
            term,
            cancellationToken);

        return brandNames
            .Select(name => new CarSuggestionResponseDto { Text = name, Type = "brand" })
            .Concat(carNames
                .Where(name => !brandNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Select(name => new CarSuggestionResponseDto { Text = name, Type = "car" }))
            .OrderBy(item => item.Type == "brand" ? 0 : 1)
            .ThenBy(item => SuggestionRank(item.Text, term))
            .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public async Task<CarResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _cars.GetCarByIdWithDetailsAsync(id, cancellationToken);
        return car?.ToResponse();
    }

    public async Task<IReadOnlyList<CarResponseDto>> GetOwnedAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var cars = await _cars.GetOwnedCarsWithDetailsAsync(ownerId, cancellationToken);
        return cars.Select(car => car.ToResponse()).ToList();
    }

    public async Task<CarResponseDto?> CreateAsync(
        string ownerId,
        SaveCarRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await _cars.UserExistsAsync(ownerId, cancellationToken)
            || !await _cars.LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        await EnsureModelMatchesBrandAsync(request.BrandId, request.Model, cancellationToken);
        EnsurePrimaryImageSafe(request);

        var now = DateTime.UtcNow;
        var car = new Car
        {
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            Model = request.Model.Trim(),
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            FuelType = request.FuelType.Trim(),
            Transmission = request.Transmission.Trim(),
            Color = VehicleColorCatalog.FromPosted(request.Color),
            Description = request.Description.Trim(),
            OwnerId = ownerId,
            Status = request.Status,
            IsFeatured = request.IsFeatured,
            IsNew = true,
            CreatedAt = now,
            Images =
            {
                new CarImage
                {
                    ImageUrl = request.ImageUrl.Trim(),
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        ApplySpecifications(car, request);
        AppendGalleryImages(car, request);

        await _cars.AddCarAsync(car, cancellationToken);
        return await GetByIdAsync(car.Id, cancellationToken);
    }

    public async Task<CarResponseDto?> CreateCatalogAsync(
        SaveCarRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await _cars.LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        await EnsureModelMatchesBrandAsync(request.BrandId, request.Model, cancellationToken);
        EnsurePrimaryImageSafe(request);

        var now = DateTime.UtcNow;
        var car = new Car
        {
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            Model = request.Model.Trim(),
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            FuelType = request.FuelType.Trim(),
            Transmission = request.Transmission.Trim(),
            Color = VehicleColorCatalog.FromPosted(request.Color),
            Description = request.Description.Trim(),
            OwnerId = null,
            Status = request.Status,
            IsFeatured = request.IsFeatured,
            IsNew = true,
            CreatedAt = now,
            Images =
            {
                new CarImage
                {
                    ImageUrl = request.ImageUrl.Trim(),
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        ApplySpecifications(car, request);
        AppendGalleryImages(car, request);

        await _cars.AddCarAsync(car, cancellationToken);
        return await GetByIdAsync(car.Id, cancellationToken);
    }

    public async Task<CarUpdateResultDto?> UpdateAsync(
        int id,
        SaveCarRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await _cars.LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        await EnsureModelMatchesBrandAsync(request.BrandId, request.Model, cancellationToken);
        EnsurePrimaryImageSafe(request);

        var car = await _cars.FindCarForUpdateAsync(id, cancellationToken);
        if (car == null)
        {
            return null;
        }

        var previousPrice = car.Price;
        car.BrandId = request.BrandId;
        car.CategoryId = request.CategoryId;
        car.Model = request.Model.Trim();
        car.Year = request.Year;
        car.Price = request.Price;
        car.Mileage = request.Mileage;
        car.FuelType = request.FuelType.Trim();
        car.Transmission = request.Transmission.Trim();
        car.Color = VehicleColorCatalog.FromPosted(request.Color);
        car.Description = request.Description.Trim();
        car.Status = request.Status;
        car.IsFeatured = request.IsFeatured;
        car.UpdatedAt = DateTime.UtcNow;
        ApplySpecifications(car, request);
        var removedImageUrls = await SyncGalleryAsync(car, request, cancellationToken);

        await _cars.SaveChangesAsync(cancellationToken);

        IReadOnlyList<NotificationResponseDto> notifications = Array.Empty<NotificationResponseDto>();
        if (Commerce.NotificationRules.IsPriceDrop(previousPrice, request.Price))
        {
            var carName = $"{car.Brand.Name} {car.Model}".Trim();
            notifications = await _notifications.CreatePriceDropNotificationsAsync(
                car.Id,
                carName,
                previousPrice,
                request.Price,
                cancellationToken);
        }

        var updated = await GetByIdAsync(id, cancellationToken);
        return updated == null ? null : new CarUpdateResultDto(updated, notifications, removedImageUrls);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _cars.DeleteCarAsync(id, cancellationToken);

    public async Task<CarLookupResponseDto> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _cars.GetBrandsOrderedAsync(cancellationToken);
        var categories = await _cars.GetCategoriesOrderedAsync(cancellationToken);
        var storedModels = await _cars.GetNonEmptyCarModelsByBrandAsync(cancellationToken);

        var brandDtos = brands.Select(brand => new BrandResponseDto
        {
            Id = brand.Id,
            Name = brand.Name,
            CreatedAt = brand.CreatedAt
        }).ToList();
        var categoryDtos = categories.Select(category => new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            IconPath = category.IconPath
        }).ToList();

        var models = new List<BrandModelResponseDto>();
        foreach (var brand in brandDtos)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in VerifiedCatalogSeeder.KnownModels.Where(item =>
                         item.Brand.Equals(brand.Name, StringComparison.OrdinalIgnoreCase)))
            {
                names.Add(item.Model);
            }

            foreach (var item in storedModels.Where(item => item.BrandId == brand.Id))
            {
                if (!string.IsNullOrWhiteSpace(item.Model))
                {
                    names.Add(item.Model.Trim());
                }
            }

            foreach (var name in names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                models.Add(new BrandModelResponseDto(brand.Id, name));
            }
        }

        return new CarLookupResponseDto(brandDtos, categoryDtos, models);
    }

    private async Task EnsureModelMatchesBrandAsync(
        int brandId,
        string model,
        CancellationToken cancellationToken)
    {
        var brand = await _cars.GetBrandByIdAsync(brandId, cancellationToken);
        if (brand == null || string.IsNullOrWhiteSpace(model))
        {
            return;
        }

        var text = model.Trim();
        if (VerifiedCatalogSeeder.BelongsToAnotherBrand(text, brand.Name))
        {
            throw new CarListingException(
                "Select a model for the chosen make, or enter a model that is not already listed under another make.");
        }

        var owners = await _cars.GetDistinctBrandIdsForModelAsync(text.ToLower(), cancellationToken);
        if (owners.Count > 0 && !owners.Contains(brandId))
        {
            throw new CarListingException(
                "Select a model for the chosen make, or enter a model that is not already listed under another make.");
        }
    }

    private static CarListQueryCriteria ToCriteria(CarListQueryDto query) =>
        new(
            query.Search,
            query.BrandId,
            query.CategoryId,
            query.Model,
            query.MinPrice,
            query.MaxPrice,
            query.MinYear,
            query.MaxYear,
            query.MinMileage,
            query.MaxMileage,
            query.SelectedSeats(),
            query.Fuel,
            query.Transmission,
            query.IsNew,
            query.Status,
            query.Sort,
            query.IncludeListingText);

    private static int SuggestionRank(string text, string term)
    {
        var value = text.ToLowerInvariant();
        if (value.StartsWith(term, StringComparison.Ordinal))
        {
            return 0;
        }

        return value.Contains(term, StringComparison.Ordinal) ? 1 : 2;
    }

    private static void AppendGalleryImages(Car car, SaveCarRequestDto request)
    {
        if (!request.GallerySpecified)
        {
            return;
        }

        var images = car.Images.ToList();
        var nextOrder = images.Count == 0 ? 0 : images.Max(image => image.DisplayOrder) + 1;
        foreach (var url in NormalizeAdditionalUrls(request, images.Count))
        {
            if (images.Any(image => CarGalleryRules.UrlsMatch(image.ImageUrl, url)))
            {
                continue;
            }

            var added = new CarImage
            {
                ImageUrl = url,
                IsMain = false,
                DisplayOrder = nextOrder++
            };
            car.Images.Add(added);
            images.Add(added);
        }
    }

    private async Task<IReadOnlyList<string>> SyncGalleryAsync(
        Car car,
        SaveCarRequestDto request,
        CancellationToken cancellationToken)
    {
        var images = car.Images.ToList();
        var removedUrls = new List<string>();
        var removeIds = request.GallerySpecified
            ? (request.RemoveImageIds ?? []).ToHashSet()
            : [];
        if (request.GallerySpecified)
        {
            var remaining = images.Count(image => !removeIds.Contains(image.Id));
            _ = NormalizeAdditionalUrls(request, remaining);
        }

        if (request.GallerySpecified)
        {
            foreach (var image in images.Where(image => removeIds.Contains(image.Id)).ToList())
            {
                removedUrls.Add(image.ImageUrl);
                car.Images.Remove(image);
                _cars.RemoveCarImage(image);
                images.Remove(image);
            }
        }

        var primaryUrl = request.ImageUrl.Trim();
        var primary = request.GallerySpecified && request.PrimaryImageId is int primaryId
            ? images.FirstOrDefault(image => image.Id == primaryId)
            : null;

        if (primary != null)
        {
            if (!CarGalleryRules.UrlsMatch(primary.ImageUrl, primaryUrl)
                && !removedUrls.Any(url => CarGalleryRules.UrlsMatch(url, primaryUrl)))
            {
                removedUrls.Add(primary.ImageUrl);
                primary.ImageUrl = primaryUrl;
            }

            SetMain(images, primary);
        }
        else
        {
            var match = images.FirstOrDefault(image => CarGalleryRules.UrlsMatch(image.ImageUrl, primaryUrl));
            if (match != null)
            {
                SetMain(images, match);
            }
            else if (removedUrls.Any(url => CarGalleryRules.UrlsMatch(url, primaryUrl)))
            {
                var fallback = images
                    .OrderBy(image => image.DisplayOrder)
                    .ThenBy(image => image.Id)
                    .FirstOrDefault();
                if (fallback != null)
                {
                    SetMain(images, fallback);
                }
            }
            else if (images.Count > 0)
            {
                var current = images.FirstOrDefault(image => image.IsMain)
                    ?? images.OrderBy(image => image.DisplayOrder).ThenBy(image => image.Id).First();
                if (!CarGalleryRules.UrlsMatch(current.ImageUrl, primaryUrl))
                {
                    removedUrls.Add(current.ImageUrl);
                    current.ImageUrl = primaryUrl;
                }

                SetMain(images, current);
            }
            else if (!string.IsNullOrWhiteSpace(primaryUrl))
            {
                var created = new CarImage
                {
                    ImageUrl = primaryUrl,
                    IsMain = true,
                    DisplayOrder = 0
                };
                car.Images.Add(created);
                images.Add(created);
            }
        }

        if (request.GallerySpecified)
        {
            var nextOrder = images.Count == 0 ? 0 : images.Max(image => image.DisplayOrder) + 1;
            foreach (var url in NormalizeAdditionalUrls(request, images.Count))
            {
                if (images.Any(image => CarGalleryRules.UrlsMatch(image.ImageUrl, url)))
                {
                    continue;
                }

                var added = new CarImage
                {
                    ImageUrl = url,
                    IsMain = false,
                    DisplayOrder = nextOrder++
                };
                car.Images.Add(added);
                images.Add(added);
            }
        }

        if (images.Count > 0 && images.All(image => !image.IsMain))
        {
            SetMain(images, images.OrderBy(image => image.DisplayOrder).ThenBy(image => image.Id).First());
        }

        if (images.Count == 0)
        {
            throw new CarGalleryException("Keep at least one image, or choose a new primary image.");
        }

        var stillUsed = new HashSet<string>(
            images.Select(image => image.ImageUrl.Trim()),
            StringComparer.OrdinalIgnoreCase);
        var candidates = removedUrls
            .Select(url => url.Trim())
            .Where(url => url.Length > 0 && !stillUsed.Contains(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0 || car.Id == 0)
        {
            return candidates;
        }

        var referenced = await _cars.GetImageUrlsUsedByOtherCarsAsync(car.Id, candidates, cancellationToken);
        var shared = new HashSet<string>(referenced, StringComparer.OrdinalIgnoreCase);
        return candidates.Where(url => !shared.Contains(url)).ToList();
    }

    private static void EnsurePrimaryImageSafe(SaveCarRequestDto request)
    {
        if (!CarGalleryRules.IsSafeReference(request.ImageUrl))
        {
            throw new CarGalleryException("The primary image path is not allowed.");
        }
    }

    private static IReadOnlyList<string> NormalizeAdditionalUrls(SaveCarRequestDto request, int existingCount)
    {
        var urls = new List<string>();
        foreach (var url in request.AdditionalImageUrls ?? [])
        {
            var trimmed = url?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (!CarGalleryRules.IsSafeReference(trimmed))
            {
                throw new CarGalleryException("An image path is not allowed.");
            }

            if (urls.Any(existing => CarGalleryRules.UrlsMatch(existing, trimmed)))
            {
                continue;
            }

            urls.Add(trimmed);
        }

        if (existingCount + urls.Count > CarGalleryRules.MaxImages)
        {
            throw new CarGalleryException($"A car can have up to {CarGalleryRules.MaxImages} images.");
        }

        return urls;
    }

    private static void SetMain(IEnumerable<CarImage> images, CarImage primary)
    {
        foreach (var image in images)
        {
            image.IsMain = ReferenceEquals(image, primary);
        }
    }

    private static void ApplySpecifications(Car car, SaveCarRequestDto request)
    {
        car.Seats = request.Seats is > 0 and <= 12 ? request.Seats : null;
        car.DriveType = CarListingRules.CanonicalDrive(request.DriveType);
        car.Engine = VehicleEngineCatalog.Normalize(request.Engine) ?? Blank(request.Engine);
        var fuel = CarListingRules.CanonicalFuel(request.FuelType);
        if (fuel != null)
        {
            car.FuelType = fuel;
        }

        var transmission = CarListingRules.CanonicalTransmission(request.Transmission);
        if (transmission != null)
        {
            car.Transmission = transmission;
        }
        car.MileageUnit = CarListingRules.CanonicalMileageUnit(request.MileageUnit);
        car.Horsepower = request.Horsepower is > 0 and <= 2000 ? request.Horsepower : null;
        car.Doors = request.Doors is >= 2 and <= 5 ? request.Doors : null;
        car.Cylinders = request.Cylinders is >= 1 and <= 16 ? request.Cylinders : null;
        car.Steering = CarListingRules.Canonical(request.Steering, CarListingRules.SteeringPositions);
        car.Vin = Blank(request.Vin)?.ToUpperInvariant();
        car.AccidentHistory = CarListingRules.Canonical(request.AccidentHistory, CarListingRules.AccidentChoices);
        car.RepaintedParts = VehicleBodyPartCatalog.Normalize(request.RepaintedParts) ?? Blank(request.RepaintedParts);
        car.ReplacedParts = VehicleBodyPartCatalog.Normalize(request.ReplacedParts) ?? Blank(request.ReplacedParts);
        car.CustomsStatus = CarListingRules.Canonical(request.CustomsStatus, CarListingRules.CustomsChoices);
        car.MarketOrigin = Blank(request.MarketOrigin);
        if (request.ConditionSpecified)
        {
            car.IsNew = string.Equals(request.Condition, "New", StringComparison.OrdinalIgnoreCase);
        }

        if (request.FeaturesSpecified)
        {
            car.Features = CarFeatureCatalog.Store(request.Features);
        }
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
