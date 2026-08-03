namespace SocialReelSaver.Domain.Enums;

/// <summary>
/// Admin hierarchy foundation (consolidated tech spec §7).
/// Permission matrix comes in a later phase.
/// </summary>
public enum AdminRole
{
    SuperAdmin = 0,
    Operations = 1,
    Support = 2,
    Technical = 3,
    Analyst = 4,
}
