namespace FinalProject.MVC.DTOs;

public sealed class AdminSummaryDto
{
    public int TotalCars { get; set; }

    public int ActiveListings { get; set; }

    public int TotalConversations { get; set; }

    public int UnreadMessages { get; set; }

    public int TotalRatings { get; set; }

    public int TotalReviews { get; set; }

    public List<string> RecentActivity { get; set; } = new();
}

public sealed class SaveLookupDto
{
    public string Name { get; set; } = string.Empty;
}
