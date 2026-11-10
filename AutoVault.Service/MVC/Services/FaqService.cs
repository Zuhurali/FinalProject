using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace FinalProject.MVC.Services;

public sealed class FaqService : IFaqService
{
    private readonly IApplicationRepository _db;

    public FaqService(IApplicationRepository db)
    {
        _db = db;
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AnyPublishedShopFaqItemsAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var sort = 100;
        foreach (var (question, answer) in ShopFaqSeedDefaults.Items)
        {
            _db.AddFaqItem(new FaqItem
            {
                Question = question,
                Answer = answer,
                SortOrder = sort,
                IsPublished = true,
                ShowOnFaqPage = false,
                ShowOnShop = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            sort += 10;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<FaqItem>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        _db.GetPublishedFaqItemsAsync(cancellationToken);

    public Task<IReadOnlyList<FaqItem>> GetShopPublishedAsync(CancellationToken cancellationToken = default) =>
        _db.GetShopPublishedFaqItemsAsync(cancellationToken);

    public Task<IReadOnlyList<FaqItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetAllFaqItemsAsync(cancellationToken);

    public Task<FaqItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetFaqItemByIdAsync(id, cancellationToken);

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
            var max = await _db.GetMaxFaqSortOrderAsync(cancellationToken) ?? 0;
            sortOrder = max + 10;
        }

        var now = DateTime.UtcNow;
        var entity = new FaqItem
        {
            Question = model.Question.Trim(),
            Answer = model.Answer.Trim(),
            SortOrder = sortOrder,
            IsPublished = model.IsPublished,
            // Legacy column: public /Faq visibility is IsPublished only. Keep a stable default for new rows.
            ShowOnFaqPage = true,
            ShowOnShop = model.ShowOnShop,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.AddFaqItem(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
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

        var entity = await _db.FindTrackedFaqItemAsync(model.Id, cancellationToken);
        if (entity == null)
        {
            return (false, "FAQ item not found.");
        }

        entity.Question = model.Question.Trim();
        entity.Answer = model.Answer.Trim();
        entity.SortOrder = model.SortOrder;
        entity.IsPublished = model.IsPublished;
        entity.ShowOnShop = model.ShowOnShop;
        entity.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            return (false, FriendlyDbError(exception));
        }

        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedFaqItemAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _db.RemoveFaqItem(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTrackedFaqItemAsync(id, cancellationToken);
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

        var items = (await _db.GetFaqItemsOrderedForMoveAsync(cancellationToken)).ToList();
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

    private static string FriendlyDbError(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
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
