namespace FinalProject.MVC.Models;

public sealed class LuxuryShowcaseBrand
{
    public required string Name { get; init; }

    public required string Descriptor { get; init; }

    public required string ImagePath { get; init; }

    public bool HasListings { get; init; }
}
