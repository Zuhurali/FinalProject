namespace FinalProject.MVC.Models;

/// <summary>Home category slider row driven by SQL-backed API categories.</summary>
public sealed class HomeCategoryStripItem
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string IconPath { get; set; } = string.Empty;

    public string CountLabel { get; set; } = "—";
}
