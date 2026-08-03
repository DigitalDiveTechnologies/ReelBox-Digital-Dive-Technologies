namespace SocialReelSaver.Api.Auth;

public static class AdminAuthConstants
{
    public const string Scheme = "AdminBearer";

    public const string PolicyAdminOnly = "AdminOnly";
    public const string PolicyUsersManage = "AdminUsers.Manage";
    public const string PolicyMediaManage = "AdminMedia.Manage";
    public const string PolicyPlatformsManage = "AdminPlatforms.Manage";
    public const string PolicySettingsManage = "AdminSettings.Manage";

    public const string PolicySuperAdmin = "AdminRole.SuperAdmin";
    public const string PolicyOperations = "AdminRole.Operations";
    public const string PolicySupport = "AdminRole.Support";
    public const string PolicyTechnical = "AdminRole.Technical";
    public const string PolicyAnalyst = "AdminRole.Analyst";
}
