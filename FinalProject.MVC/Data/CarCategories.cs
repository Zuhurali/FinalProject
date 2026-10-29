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

    /// <summary>
    /// True when a SQL/API category name matches a request value (name, slug, or legacy alias).
    /// </summary>
    public static bool Matches(string? storedCategoryName, string? requested)
    {
        if (string.IsNullOrWhiteSpace(storedCategoryName) || string.IsNullOrWhiteSpace(requested))
        {
            return false;
        }

        var stored = storedCategoryName.Trim();
        var request = requested.Trim();
        if (stored.Equals(request, StringComparison.OrdinalIgnoreCase)
            || Slug(stored).Equals(request, StringComparison.OrdinalIgnoreCase)
            || stored.Equals(request.Replace('-', ' '), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var alias = Resolve(request);
        return alias != null
            && (stored.Equals(alias.Value, StringComparison.OrdinalIgnoreCase)
                || stored.Equals(alias.DisplayName, StringComparison.OrdinalIgnoreCase));
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
