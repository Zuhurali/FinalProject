using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

public static class CampaignBannerSelection
{
    public const int HomeBannerLimit = 3;

    /// <summary>
    /// Homepage banners: all live campaigns (single-part and all-parts),
    /// newest start first, Id tie-break, max 3.
    /// </summary>
    public static IReadOnlyList<ProductCampaign> ForHome(IEnumerable<ProductCampaign> liveCampaigns)
    {
        return liveCampaigns
            .OrderByDescending(campaign => campaign.StartsAt)
            .ThenBy(campaign => campaign.Id)
            .Take(HomeBannerLimit)
            .ToList();
    }
}
