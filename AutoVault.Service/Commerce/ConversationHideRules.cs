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

    /// <summary>
    /// Only the conversation buyer or seller may hide a thread from their own inbox.
    /// Role/staff flags do not grant hide rights.
    /// </summary>
    public static bool CanHideForUser(
        string userId,
        string buyerId,
        string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return string.Equals(userId, buyerId, StringComparison.Ordinal)
            || string.Equals(userId, sellerId, StringComparison.Ordinal);
    }
}
