namespace FinalProject.DTOs;

public sealed record AdminSummaryResponse(
    int TotalCars,
    int ActiveListings,
    int TotalConversations,
    int UnreadMessages,
    int TotalRatings,
    IReadOnlyList<string> RecentActivity);

public sealed class SaveLookupRequest
{
    public string Name { get; set; } = string.Empty;
}
