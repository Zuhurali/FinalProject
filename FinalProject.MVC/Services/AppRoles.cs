namespace FinalProject.MVC.Services;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string LegacyUser = "User";

    public static readonly string[] All = [SuperAdmin, Admin, Member];
}
