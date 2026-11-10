using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class TeamMemberService : ITeamMemberService
{
    private readonly IApplicationRepository _db;
    private readonly IWebHostEnvironment _environment;

    public TeamMemberService(IApplicationRepository db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public Task<IReadOnlyList<TeamMember>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        _db.GetPublishedTeamMembersAsync(cancellationToken);

    public Task<IReadOnlyList<TeamMember>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetAllTeamMembersAsync(cancellationToken);

    public Task<TeamMember?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTeamMemberByIdAsync(id, cancellationToken);

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyTeamMembersAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var members = TeamCatalog.Members.Select(catalog => new TeamMember
        {
            Id = catalog.Id,
            Name = catalog.Name,
            Position = catalog.Position,
            ImagePath = catalog.ImagePath,
            Biography = catalog.Biography,
            Experience = catalog.Experience,
            Department = catalog.Department,
            Phone = catalog.Phone,
            Email = catalog.Email,
            SortOrder = catalog.Id * 10,
            IsPublished = true,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        await _db.SeedTeamMembersWithIdentityInsertAsync(members, cancellationToken);
    }

    public async Task<(bool Ok, string? Error, int? Id)> CreateAsync(
        TeamMemberEditViewModel model,
        IFormFile? image,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePhone(model, out var phoneError))
        {
            return (false, phoneError, null);
        }

        var (path, pathError) = await ResolveImagePathAsync(model.ImagePath, image, cancellationToken);
        if (pathError != null)
        {
            return (false, pathError, null);
        }

        var sortOrder = model.SortOrder;
        if (sortOrder <= 0)
        {
            var max = await _db.GetMaxTeamMemberSortOrderAsync(cancellationToken) ?? 0;
            sortOrder = max + 10;
        }

        var now = DateTime.UtcNow;
        var entity = MapToEntity(model, path);
        entity.SortOrder = sortOrder;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        _db.AddTeamMember(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, entity.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(
        TeamMemberEditViewModel model,
        IFormFile? image,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePhone(model, out var phoneError))
        {
            return (false, phoneError);
        }

        var entity = await _db.FindTrackedTeamMemberAsync(model.Id, cancellationToken);
        if (entity == null)
        {
            return (false, "Team member not found.");
        }

        var (path, pathError) = await ResolveImagePathAsync(
            string.IsNullOrWhiteSpace(model.ImagePath) ? entity.ImagePath : model.ImagePath,
            image,
            cancellationToken);
        if (pathError != null)
        {
            return (false, pathError);
        }

        entity.Name = model.Name.Trim();
        entity.Position = model.Position.Trim();
        entity.ImagePath = path;
        entity.Biography = model.Biography.Trim();
        entity.Experience = model.Experience.Trim();
        entity.Department = model.Department.Trim();
        entity.Phone = model.Phone.Trim();
        entity.Email = model.Email.Trim();
        entity.SortOrder = model.SortOrder;
        entity.IsPublished = model.IsPublished;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedTeamMemberAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _db.RemoveTeamMember(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedTeamMemberAsync(id, cancellationToken);
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

        var items = (await _db.GetTeamMembersOrderedAsync(cancellationToken)).ToList();
        var index = items.FindIndex(m => m.Id == id);
        if (index < 0)
        {
            return (false, "Team member not found.");
        }

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= items.Count)
        {
            return (false, "This team member is already at the edge of the list.");
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
        var path = currentPath?.Trim() ?? string.Empty;
        var upload = await CmsAssetUpload.TrySaveAsync(
            _environment,
            TeamMemberAssetRules.UploadFolderRelative,
            TeamMemberAssetRules.UploadFolderWeb,
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

        var pathError = TeamMemberAssetRules.ValidateStoredPath(path);
        return pathError != null ? (path, pathError) : (path, null);
    }

    private static bool TryNormalizePhone(TeamMemberEditViewModel model, out string? error)
    {
        if (!AzerbaijanPhoneNumber.TryNormalize(model.Phone, out var phone))
        {
            error = AzerbaijanPhoneNumber.ErrorMessage;
            return false;
        }

        model.Phone = phone;
        error = null;
        return true;
    }

    private static TeamMember MapToEntity(TeamMemberEditViewModel model, string imagePath) => new()
    {
        Name = model.Name?.Trim() ?? string.Empty,
        Position = model.Position?.Trim() ?? string.Empty,
        ImagePath = imagePath,
        Biography = model.Biography?.Trim() ?? string.Empty,
        Experience = model.Experience?.Trim() ?? string.Empty,
        Department = model.Department?.Trim() ?? string.Empty,
        Phone = model.Phone?.Trim() ?? string.Empty,
        Email = model.Email?.Trim() ?? string.Empty,
        IsPublished = model.IsPublished
    };
}
