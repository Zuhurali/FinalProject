using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public class CarService : ICarService
{
    private readonly FinalProjectDbContext _dbContext;
    private readonly INotificationService _notifications;

    public CarService(FinalProjectDbContext dbContext, INotificationService notifications)
    {
        _dbContext = dbContext;
        _notifications = notifications;
    }

    public async Task<PagedResult<CarResponse>> QueryAsync(CarListQuery query, CancellationToken cancellationToken = default)
    {
        var filtered = ApplyFilters(_dbContext.Cars.AsNoTracking(), query);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        IReadOnlyList<CarResponse> items = Array.Empty<CarResponse>();
        if (totalCount > 0 && query.Page <= totalPages)
        {
            var cars = await filtered
                .OrderBy(car => car.Brand.Name)
                .ThenBy(car => car.Model)
                .ThenByDescending(car => car.Year)
                .ThenByDescending(car => car.CreatedAt)
                .ThenBy(car => car.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(car => car.Brand)
                .Include(car => car.Category)
                .Include(car => car.Owner)
                .Include(car => car.Images)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            items = cars.Select(car => car.ToResponse()).ToList();
        }

        return new PagedResult<CarResponse>
        {
            Items = items,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<IReadOnlyList<CarSuggestionResponse>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var term = query.Trim().ToLower();
        var brandNames = await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Name.ToLower().Contains(term))
            .Select(brand => brand.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var carNames = await _dbContext.Cars
            .AsNoTracking()
            .Where(car =>
                car.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower()
                && (car.Description.ToLower().Contains(term)
                    || car.Model.ToLower().Contains(term)
                    || car.Brand.Name.ToLower().Contains(term)))
            .Select(car => car.Description)
            .Where(name => name != "")
            .Distinct()
            .ToListAsync(cancellationToken);

        return brandNames
            .Select(name => new CarSuggestionResponse { Text = name, Type = "brand" })
            .Concat(carNames
                .Where(name => !brandNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Select(name => new CarSuggestionResponse { Text = name, Type = "car" }))
            .OrderBy(item => item.Type == "brand" ? 0 : 1)
            .ThenBy(item => SuggestionRank(item.Text, term))
            .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public async Task<CarResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _dbContext.Cars
            .AsNoTracking()
            .Include(item => item.Brand)
            .Include(item => item.Category)
            .Include(item => item.Owner)
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return car?.ToResponse();
    }

    public async Task<IReadOnlyList<CarResponse>> GetOwnedAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var cars = await _dbContext.Cars
            .AsNoTracking()
            .Where(car => car.OwnerId == ownerId)
            .OrderByDescending(car => car.CreatedAt)
            .Include(car => car.Brand)
            .Include(car => car.Category)
            .Include(car => car.Owner)
            .Include(car => car.Images)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return cars.Select(car => car.ToResponse()).ToList();
    }

    public async Task<CarResponse?> CreateAsync(
        string ownerId,
        SaveCarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.IdentityUsers.AnyAsync(user => user.Id == ownerId, cancellationToken)
            || !await LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        await EnsureModelMatchesBrandAsync(request.BrandId, request.Model, cancellationToken);
        EnsurePrimaryImageSafe(request);

        var now = DateTime.UtcNow;
        var car = new Models.Entities.Car
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
            IsNew = true,
            CreatedAt = now,
            Images =
            {
                new Models.Entities.CarImage
                {
                    ImageUrl = request.ImageUrl.Trim(),
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        ApplySpecifications(car, request);
        AppendGalleryImages(car, request);

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(car.Id, cancellationToken);
    }

    public async Task<CarResponse?> CreateCatalogAsync(
        SaveCarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        await EnsureModelMatchesBrandAsync(request.BrandId, request.Model, cancellationToken);
        EnsurePrimaryImageSafe(request);

        var now = DateTime.UtcNow;
        var car = new Models.Entities.Car
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
            IsNew = true,
            CreatedAt = now,
            Images =
            {
                new Models.Entities.CarImage
                {
                    ImageUrl = request.ImageUrl.Trim(),
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };
        ApplySpecifications(car, request);
        AppendGalleryImages(car, request);

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(car.Id, cancellationToken);
    }

    public async Task<CarUpdateResult?> UpdateAsync(
        int id,
        SaveCarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        await EnsureModelMatchesBrandAsync(request.BrandId, request.Model, cancellationToken);
        EnsurePrimaryImageSafe(request);

        var car = await _dbContext.Cars
            .Include(item => item.Images)
            .Include(item => item.Brand)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
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
        car.UpdatedAt = DateTime.UtcNow;
        ApplySpecifications(car, request);
        var removedImageUrls = await SyncGalleryAsync(car, request, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        IReadOnlyList<NotificationResponse> notifications = Array.Empty<NotificationResponse>();
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
        return updated == null ? null : new CarUpdateResult(updated, notifications, removedImageUrls);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _dbContext.Cars.FindAsync([id], cancellationToken);
        if (car == null)
        {
            return false;
        }

        _dbContext.Cars.Remove(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CarLookupResponse> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .Select(brand => new BrandResponse
            {
                Id = brand.Id,
                Name = brand.Name,
                CreatedAt = brand.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);
        var storedModels = await _dbContext.Cars
            .AsNoTracking()
            .Where(car => car.Model != "")
            .Select(car => new { car.BrandId, car.Model })
            .ToListAsync(cancellationToken);
        var models = new List<BrandModelResponse>();
        foreach (var brand in brands)
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
                models.Add(new BrandModelResponse(brand.Id, name));
            }
        }

        return new CarLookupResponse(brands, categories, models);
    }

    private async Task EnsureModelMatchesBrandAsync(
        int brandId,
        string model,
        CancellationToken cancellationToken)
    {
        var brand = await _dbContext.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == brandId, cancellationToken);
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

        var owners = await _dbContext.Cars
            .AsNoTracking()
            .Where(car => car.Model.ToLower() == text.ToLower())
            .Select(car => car.BrandId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (owners.Count > 0 && !owners.Contains(brandId))
        {
            throw new CarListingException(
                "Select a model for the chosen make, or enter a model that is not already listed under another make.");
        }
    }

    private async Task<bool> LookupsExistAsync(
        int brandId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Brands.AnyAsync(brand => brand.Id == brandId, cancellationToken)
            && await _dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    private static IQueryable<Models.Entities.Car> ApplyFilters(IQueryable<Models.Entities.Car> cars, CarListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                var token = term;
                cars = cars.Where(car =>
                    car.Brand.Name.ToLower().Contains(token)
                    || car.Model.ToLower().Contains(token)
                    || car.Description.ToLower().Contains(token));
            }
        }

        if (query.BrandId is int brandId)
        {
            cars = cars.Where(car => car.BrandId == brandId);
        }

        if (query.CategoryId is int categoryId)
        {
            cars = cars.Where(car => car.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Model))
        {
            var model = query.Model.Trim().ToLower();
            cars = cars.Where(car => car.Model.ToLower() == model);
        }

        if (query.MinPrice is decimal minPrice)
        {
            cars = cars.Where(car => car.Price >= minPrice);
        }

        if (query.MaxPrice is decimal maxPrice)
        {
            cars = cars.Where(car => car.Price <= maxPrice);
        }

        if (query.MinYear is int minYear)
        {
            cars = cars.Where(car => car.Year >= minYear);
        }

        if (query.MaxYear is int maxYear)
        {
            cars = cars.Where(car => car.Year <= maxYear);
        }

        if (!string.IsNullOrWhiteSpace(query.Fuel))
        {
            var fuel = query.Fuel.Trim().ToLower();
            cars = cars.Where(car => car.FuelType.ToLower() == fuel);
        }

        if (!string.IsNullOrWhiteSpace(query.Transmission))
        {
            var transmission = query.Transmission.Trim().ToLower();
            cars = cars.Where(car => car.Transmission.ToLower() == transmission);
        }

        if (query.IsNew is bool isNew)
        {
            cars = cars.Where(car => car.IsNew == isNew);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLower();
            cars = cars.Where(car => car.Status.ToLower() == status);
        }

        return cars;
    }

    private static int SuggestionRank(string text, string term)
    {
        var value = text.ToLowerInvariant();
        if (value.StartsWith(term, StringComparison.Ordinal))
        {
            return 0;
        }

        return value.Contains(term, StringComparison.Ordinal) ? 1 : 2;
    }

    private static void AppendGalleryImages(Models.Entities.Car car, SaveCarRequest request)
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

            var added = new Models.Entities.CarImage
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
        Models.Entities.Car car,
        SaveCarRequest request,
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
                _dbContext.Remove(image);
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
                var created = new Models.Entities.CarImage
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

                var added = new Models.Entities.CarImage
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

        var referenced = await _dbContext.CarImages
            .AsNoTracking()
            .Where(image => image.CarId != car.Id && candidates.Contains(image.ImageUrl))
            .Select(image => image.ImageUrl)
            .ToListAsync(cancellationToken);
        var shared = new HashSet<string>(referenced, StringComparer.OrdinalIgnoreCase);
        return candidates.Where(url => !shared.Contains(url)).ToList();
    }

    private static void EnsurePrimaryImageSafe(SaveCarRequest request)
    {
        if (!CarGalleryRules.IsSafeReference(request.ImageUrl))
        {
            throw new CarGalleryException("The primary image path is not allowed.");
        }
    }

    private static IReadOnlyList<string> NormalizeAdditionalUrls(SaveCarRequest request, int existingCount)
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

    private static void SetMain(IEnumerable<Models.Entities.CarImage> images, Models.Entities.CarImage primary)
    {
        foreach (var image in images)
        {
            image.IsMain = ReferenceEquals(image, primary);
        }
    }

    private static void ApplySpecifications(Models.Entities.Car car, SaveCarRequest request)
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
