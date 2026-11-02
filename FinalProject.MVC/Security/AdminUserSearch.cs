namespace FinalProject.MVC.Security;

/// <summary>
/// Pure search matching for the SuperAdmin Users list (no database access).
/// </summary>
public static class AdminUserSearch
{
    public static bool Matches(
        string? search,
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        return Contains(firstName, term)
            || Contains(lastName, term)
            || Contains(email, term)
            || Contains(phoneNumber, term);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
