namespace FinalProject.MVC.Models;

public sealed class CampaignFormInput
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? AppliesToAllParts { get; set; }

    public string? ProductId { get; set; }

    public string? DiscountPercent { get; set; }

    public string? StartsAt { get; set; }

    public string? EndsAt { get; set; }
}
