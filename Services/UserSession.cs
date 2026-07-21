using GymManagement.Models;

namespace GymManagement.Services;

public class UserSession
{
    private static UserSession? _instance;
    public static UserSession Instance => _instance ??= new UserSession();

    public User? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;
    public string? CurrentRole => CurrentUser?.Role;

    public bool IsInRole(params string[] roles)
        => CurrentUser != null && roles.Any(role =>
            string.Equals(role, CurrentUser.Role, StringComparison.OrdinalIgnoreCase));

    public bool CanManageMembers => IsInRole(UserRoles.Admin, UserRoles.Receptionist);
    public bool CanManagePos => IsInRole(UserRoles.Admin, UserRoles.Receptionist);
    public bool CanManageBookings => IsInRole(UserRoles.Admin, UserRoles.Receptionist);
    public bool CanManageEquipment => IsInRole(UserRoles.Admin);
    public bool CanViewOwnPtBookings => IsInRole(UserRoles.Admin, UserRoles.Receptionist, UserRoles.Pt, UserRoles.Member);
    public bool CanViewAnalytics => IsInRole(UserRoles.Admin);

    public void Login(User user) => CurrentUser = user;
    public void Logout() => CurrentUser = null;
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Pt = "PT";
    public const string Member = "Member";

    public static bool IsKnown(string? role)
        => role is Admin or Receptionist or Pt or Member;
}
