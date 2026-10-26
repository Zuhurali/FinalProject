using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class FaqService : IFaqService
{
    private readonly ApplicationDbContext _db;

    public FaqService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FaqItem>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        await _db.FaqItems.AsNoTracking()
            .Where(item => item.IsPublished)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FaqItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.FaqItems.AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public Task<FaqItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.FaqItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<(bool Ok, string? Error, int? Id)> CreateAsync(
        FaqItemEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(model);
        if (validation != null)
        {
            return (false, validation, null);
        }

        var sortOrder = model.SortOrder;
        if (sortOrder <= 0)
        {
            var max = await _db.FaqItems.Select(item => (int?)item.SortOrder).MaxAsync(cancellationToken) ?? 0;
            sortOrder = max + 10;
        }

        var now = DateTime.UtcNow;
        var entity = new FaqItem
        {
            Question = model.Question.Trim(),
            Answer = model.Answer.Trim(),
            SortOrder = sortOrder,
            IsPublished = model.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.FaqItems.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return (false, FriendlyDbError(exception), null);
        }

        return (true, null, entity.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(
        FaqItemEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(model);
        if (validation != null)
        {
            return (false, validation);
        }

        var entity = await _db.FaqItems.FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken);
        if (entity == null)
        {
            return (false, "FAQ item not found.");
        }

        entity.Question = model.Question.Trim();
        entity.Answer = model.Answer.Trim();
        entity.SortOrder = model.SortOrder;
        entity.IsPublished = model.IsPublished;
        entity.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return (false, FriendlyDbError(exception));
        }

        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FaqItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _db.FaqItems.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FaqItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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

        var items = await _db.FaqItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var index = items.FindIndex(item => item.Id == id);
        if (index < 0)
        {
            return (false, "FAQ item not found.");
        }

        var targetIndex = index + direction;
        if (targetIndex < 0)
        {
            return (false, "This FAQ is already at the top.");
        }

        if (targetIndex >= items.Count)
        {
            return (false, "This FAQ is already at the bottom.");
        }

        var now = DateTime.UtcNow;
        var current = items[index];
        var neighbor = items[targetIndex];

        if (current.SortOrder != neighbor.SortOrder)
        {
            (current.SortOrder, neighbor.SortOrder) = (neighbor.SortOrder, current.SortOrder);
            current.UpdatedAt = now;
            neighbor.UpdatedAt = now;
        }
        else
        {
            // Tied SortOrder values: reorder in memory, then normalize so the move is visible
            // and stable even when several rows share the same order.
            items.RemoveAt(index);
            items.Insert(targetIndex, current);
            for (var i = 0; i < items.Count; i++)
            {
                var desiredOrder = (i + 1) * 10;
                if (items[i].SortOrder == desiredOrder)
                {
                    continue;
                }

                items[i].SortOrder = desiredOrder;
                items[i].UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private static string? Validate(FaqItemEditViewModel model)
    {
        var questionError = FaqContentRules.ValidateQuestion(model.Question);
        if (questionError != null)
        {
            return questionError;
        }

        var answerError = FaqContentRules.ValidateAnswer(model.Answer);
        if (answerError != null)
        {
            return answerError;
        }

        if (model.SortOrder is < 0 or > 100000)
        {
            return "Sort order must be between 0 and 100000.";
        }

        return null;
    }

    private static string FriendlyDbError(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current != null; current = current.InnerException)
        {
            if (current is SqlException sql)
            {
                var message = sql.Message ?? string.Empty;
                if (message.Contains("CK_FaqItems_QuestionLength", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Question must be between {FaqContentRules.QuestionMinLength} and {FaqContentRules.QuestionMaxLength} characters.";
                }

                if (message.Contains("CK_FaqItems_AnswerLength", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Answer must be between {FaqContentRules.AnswerMinLength} and {FaqContentRules.AnswerMaxLength} characters.";
                }
            }
        }

        return "The FAQ could not be saved. Check the question and answer lengths and try again.";
    }
}
