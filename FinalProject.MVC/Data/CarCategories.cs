namespace FinalProject.MVC.Data;

public sealed class CarCategoryDefinition
{
    public CarCategoryDefinition(string displayName, string slug, string value)
    {
        DisplayName = displayName;
        Slug = slug;
        Value = value;
    }

    public string DisplayName { get; }

    public string Slug { get; }

    public string Value { get; }
}

public static class CarCategories
{
    public static readonly IReadOnlyList<CarCategoryDefinition> All = new[]
    {
        Define("Convertible", "Convertible", "Convertible"),
        Define("Sports Car", "Sports-Car", "Sports Car"),
        Define("Hatchback", "Hatchback", "Hatchback"),
        Define("Minivans", "Minivans", "Minivans"),
        Define("Luxury Cars", "Luxury-Cars", "Luxury Cars"),
        Define("Sedans", "Sedans", "Sedan"),
        Define("SUV", "SUV", "SUV"),
        Define("Coupe", "Coupe", "Coupe"),
        Define("Electric", "Electric", "Electric")
    };

    public static string Slug(string displayName)
    {
        var match = Find(displayName);
        return match?.Slug ?? displayName.Replace(' ', '-');
    }

    public static CarCategoryDefinition? Resolve(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return Find(text);
    }

    private static CarCategoryDefinition? Find(string text)
    {
        var normalized = text.Trim().Replace('-', ' ');
        return All.FirstOrDefault(category =>
            category.Slug.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase)
            || category.DisplayName.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || category.Value.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || category.DisplayName.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase)
            || category.Value.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static CarCategoryDefinition Define(string displayName, string slug, string value)
    {
        return new CarCategoryDefinition(displayName, slug, value);
    }
}
