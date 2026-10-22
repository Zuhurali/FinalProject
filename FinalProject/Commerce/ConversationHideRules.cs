namespace FinalProject.Commerce;

/// <summary>
/// Pure rules for per-user conversation list hides (no database access).
/// </summary>
public static class ConversationHideRules
{
    public static bool IsHiddenForUser(
        IEnumerable<(int ConversationId, string UserId)> hides,
        int conversationId,
        string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return hides.Any(hide =>
            hide.ConversationId == conversationId
            && string.Equals(hide.UserId, userId, StringComparison.Ordinal));
    }

    public static bool CanHideForUser(
        string userId,
        string buyerId,
        string sellerId,
        bool asStoreStaff,
        string? storeInboxUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (string.Equals(userId, buyerId, StringComparison.Ordinal)
            || string.Equals(userId, sellerId, StringComparison.Ordinal))
        {
            return true;
        }

        return asStoreStaff
            && !string.IsNullOrWhiteSpace(storeInboxUserId)
            && string.Equals(sellerId, storeInboxUserId, StringComparison.Ordinal);
    }
}
