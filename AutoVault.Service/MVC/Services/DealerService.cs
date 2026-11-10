using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class DealerService : IDealerService
{
    private readonly IApplicationRepository _db;
    private readonly IWebHostEnvironment _environment;

    public DealerService(IApplicationRepository db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public Task<IReadOnlyList<Dealer>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        _db.GetPublishedDealersAsync(cancellationToken);

    public Task<IReadOnlyList<Dealer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetAllDealersAsync(cancellationToken);

    public Task<Dealer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetDealerByIdAsync(id, cancellationToken);

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyDealersAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var dealers = DealerCatalog.All.Select(catalog => new Dealer
            {
                Id = catalog.Id,
                Name = catalog.Name,
                CardImage = catalog.CardImage,
                DetailImage = catalog.DetailImage,
                Summary = catalog.Summary,
                Phone = catalog.Phone,
                Email = catalog.Email,
                Address = catalog.Address,
                Description = catalog.Description,
                DescriptionExtra = catalog.DescriptionExtra,
                FeatureList = DealerFeatureDefaults.FeatureList,
                CarType = catalog.CarType,
                Mileage = catalog.Mileage,
                FuelType = catalog.FuelType,
                Transmission = catalog.Transmission,
                Person = catalog.Person,
                DriveType = catalog.DriveType,
                Engine = catalog.Engine,
                Color = catalog.Color,
                ContactName = catalog.ContactName,
                ContactRole = catalog.ContactRole,
                Whatsapp = catalog.Whatsapp,
                ContactPhone = catalog.ContactPhone,
                SpecTransmission = catalog.SpecTransmission,
                ChargingTime = catalog.ChargingTime,
                BatteryCapacity = catalog.BatteryCapacity,
                FastCharge = catalog.FastCharge,
                BatteryWarranty = catalog.BatteryWarranty,
                PowertrainWarranty = catalog.PowertrainWarranty,
                Displacement = catalog.Displacement,
                Torque = catalog.Torque,
                Horsepower = catalog.Horsepower,
                Cylinders = catalog.Cylinders,
                EngineLayout = catalog.EngineLayout,
                EngineConfig = catalog.EngineConfig,
                SortOrder = catalog.Id * 10,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

        await _db.SeedDealersWithIdentityInsertAsync(dealers, cancellationToken);
    }

    public async Task<(bool Ok, string? Error, int? Id)> CreateAsync(
        DealerEditViewModel model,
        DealerImageUploads uploads,
        CancellationToken cancellationToken = default)
    {
        var card = await ResolveImagePathAsync(model.CardImage, uploads.CardImage, cancellationToken);
        if (card.Error != null) return (false, card.Error, null);
        var detail = await ResolveImagePathAsync(model.DetailImage, uploads.DetailImage, cancellationToken);
        if (detail.Error != null) return (false, detail.Error, null);

        var sortOrder = model.SortOrder;
        if (sortOrder <= 0)
        {
            var max = await _db.GetMaxDealerSortOrderAsync(cancellationToken) ?? 0;
            sortOrder = max + 10;
        }

        var now = DateTime.UtcNow;
        var entity = MapToEntity(model, card.Path, detail.Path);
        entity.SortOrder = sortOrder;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        _db.AddDealer(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, entity.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(
        DealerEditViewModel model,
        DealerImageUploads uploads,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedDealerAsync(model.Id, cancellationToken);
        if (entity == null)
        {
            return (false, "Dealer not found.");
        }

        var card = await ResolveImagePathAsync(
            string.IsNullOrWhiteSpace(model.CardImage) ? entity.CardImage : model.CardImage,
            uploads.CardImage,
            cancellationToken);
        if (card.Error != null) return (false, card.Error);
        var detail = await ResolveImagePathAsync(
            string.IsNullOrWhiteSpace(model.DetailImage) ? entity.DetailImage : model.DetailImage,
            uploads.DetailImage,
            cancellationToken);
        if (detail.Error != null) return (false, detail.Error);

        ApplyModel(entity, model, card.Path, detail.Path);
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedDealerAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _db.RemoveDealer(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedDealerAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        entity.IsPublished = isPublished;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Ok, string? Error)> MoveAsync(
        int id,
        int direction,
        CancellationToken cancellationToken = default)
    {
        if (direction is not (1 or -1))
        {
            return (false, "Invalid move direction.");
        }

        var items = (await _db.GetDealersOrderedAsync(cancellationToken)).ToList();
        var index = items.FindIndex(d => d.Id == id);
        if (index < 0)
        {
            return (false, "Dealer not found.");
        }

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= items.Count)
        {
            return (false, "This dealer is already at the edge of the list.");
        }

        var now = DateTime.UtcNow;
        var current = items[index];
        var neighbor = items[targetIndex];
        (current.SortOrder, neighbor.SortOrder) = (neighbor.SortOrder, current.SortOrder);
        current.UpdatedAt = now;
        neighbor.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<(string Path, string? Error)> ResolveImagePathAsync(
        string currentPath,
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        var path = currentPath.Trim();
        var upload = await CmsAssetUpload.TrySaveAsync(
            _environment,
            DealerAssetRules.UploadFolderRelative,
            DealerAssetRules.UploadFolderWeb,
            image,
            cancellationToken);
        if (upload.Error != null)
        {
            return (path, upload.Error);
        }

        if (upload.Path != null)
        {
            path = upload.Path;
        }

        var pathError = DealerAssetRules.ValidateStoredPath(path);
        return pathError != null ? (path, pathError) : (path, null);
    }

    private static Dealer MapToEntity(DealerEditViewModel model, string cardImage, string detailImage)
    {
        var entity = new Dealer();
        ApplyModel(entity, model, cardImage, detailImage);
        return entity;
    }

    private static void ApplyModel(Dealer entity, DealerEditViewModel model, string cardImage, string detailImage)
    {
        entity.Name = model.Name.Trim();
        entity.CardImage = cardImage;
        entity.DetailImage = detailImage;
        entity.Summary = model.Summary.Trim();
        entity.Phone = model.Phone.Trim();
        entity.Email = model.Email.Trim();
        entity.Address = model.Address.Trim();
        entity.Description = model.Description.Trim();
        entity.DescriptionExtra = model.DescriptionExtra.Trim();
        entity.FeatureList = model.FeatureList.Trim();
        entity.CarType = model.CarType.Trim();
        entity.Mileage = model.Mileage.Trim();
        entity.FuelType = model.FuelType.Trim();
        entity.Transmission = model.Transmission.Trim();
        entity.Person = model.Person.Trim();
        entity.DriveType = model.DriveType.Trim();
        entity.Engine = model.Engine.Trim();
        entity.Color = model.Color.Trim();
        entity.ContactName = model.ContactName.Trim();
        entity.ContactRole = model.ContactRole.Trim();
        entity.Whatsapp = model.Whatsapp.Trim();
        entity.ContactPhone = model.ContactPhone.Trim();
        entity.SpecTransmission = model.SpecTransmission.Trim();
        entity.ChargingTime = model.ChargingTime.Trim();
        entity.BatteryCapacity = model.BatteryCapacity.Trim();
        entity.FastCharge = model.FastCharge.Trim();
        entity.BatteryWarranty = model.BatteryWarranty.Trim();
        entity.PowertrainWarranty = model.PowertrainWarranty.Trim();
        entity.Displacement = model.Displacement.Trim();
        entity.Torque = model.Torque.Trim();
        entity.Horsepower = model.Horsepower.Trim();
        entity.Cylinders = model.Cylinders.Trim();
        entity.EngineLayout = model.EngineLayout.Trim();
        entity.EngineConfig = model.EngineConfig.Trim();
        entity.SortOrder = model.SortOrder;
        entity.IsPublished = model.IsPublished;
    }
}
