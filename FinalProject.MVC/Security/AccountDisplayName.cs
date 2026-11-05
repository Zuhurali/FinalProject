namespace FinalProject.MVC.Security;

public static class AccountDisplayName
{
    public static string Resolve(string? firstName, string? lastName, string? userName, string? email)
    {
        var fullName = string.Join(
            " ",
            new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
        if (fullName.Length > 0)
        {
            return fullName;
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return email.Trim();
        }

        return "Account";
    }
}
