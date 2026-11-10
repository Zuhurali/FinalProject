namespace FinalProject.Data;

/// <summary>
/// FinalProjectDb is one SQL database with two EF contexts and two migration histories.
/// Merging them would rewrite Identity and the existing history tables, so they stay separate.
/// Apply MVC migrations only from FinalProject.MVC, and API migrations only from FinalProject.
/// Never point either command at the other history table, and never update FinalProjectDb
/// from a generated migration that was not reviewed against the owner below.
/// </summary>
public static class DatabaseOwnership
{
    public const string DatabaseName = "FinalProjectDb";

    public const string ApiHistoryTable = "__ApiMigrationsHistory";

    public const string MvcHistoryTable = "__EFMigrationsHistory";

    public const string ApiMigrationsProject = "FinalProject";

    public const string MvcMigrationsProject = "FinalProject.MVC";

    /// <summary>Tables created and changed only by FinalProjectDbContext migrations.</summary>
    public static readonly string[] ApiOwnedTables =
    [
        "Brands",
        "Categories",
        "Cars",
        "CarImages",
        "Favorites",
        "ProductFavorites",
        "ProductRatings",
        "ProductReviews",
        "ProductReviewRevisions",
        "CarViews",
        "Conversations",
        "ConversationHides",
        "Messages",
        "UserNotifications"
    ];

    /// <summary>
    /// Tables created and changed only by ApplicationDbContext migrations.
    /// The API may read some of these through reference entities, which are excluded from API migrations.
    /// </summary>
    public static readonly string[] MvcOwnedTables =
    [
        "AspNetUsers",
        "AspNetRoles",
        "AspNetUserRoles",
        "AspNetUserClaims",
        "AspNetUserLogins",
        "AspNetUserTokens",
        "AspNetRoleClaims",
        "Coupons",
        "CouponUsages",
        "Orders",
        "OrderItems",
        "ShopProducts",
        "ProductCampaigns",
        "InPersonSales",
        "ProductInquiries",
        "ShowroomPurchaseRequests",
        "ContactRequests",
        "SiteSettings",
        "HomePageContents",
        "FaqItems",
        "AboutPageContents",
        "ContactPageContents",
        "LegalPageContents",
        "ShopMarketingContents",
        "TeamMembers",
        "Dealers",
        "PartnerStripSettings",
        "PartnerStripItems",
        "FaqPageContents",
        "TeamPageContents",
        "DealerPageContents",
        "CalculatorPageContents"
    ];

    /// <summary>Physical tables the API reads but must not migrate.</summary>
    public static readonly string[] ApiReadOnlySharedTables =
    [
        "AspNetUsers",
        "ShopProducts",
        "ShowroomPurchaseRequests",
        "InPersonSales",
        "ProductCampaigns"
    ];
}
