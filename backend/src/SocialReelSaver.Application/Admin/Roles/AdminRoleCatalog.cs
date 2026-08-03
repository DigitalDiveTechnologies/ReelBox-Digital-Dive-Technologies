using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Admin.Roles;

public static class AdminRoleCatalog
{
    private static readonly string[] AllPhase6 =
    [
        "media.read", "media.manage",
        "jobs.read", "jobs.manage",
        "platforms.read", "platforms.manage",
        "providers.read", "providers.manage",
        "storage.read", "storage.manage",
        "reports.read", "reports.export",
        "health.read",
        "logs.read",
        "settings.read", "settings.manage",
    ];

    public static readonly IReadOnlyList<RoleDefinitionResponse> Definitions =
    [
        new("SuperAdmin", "Full administrative access.",
        [
            "dashboard.read", "users.read", "users.manage", "admins.read", "admins.manage",
            "roles.read", "roles.assign", "audit.read",
            ..AllPhase6
        ]),
        new("Operations", "Operational visibility and control.",
        [
            "dashboard.read", "users.read", "audit.read",
            "media.read", "media.manage", "jobs.read", "jobs.manage",
            "platforms.read", "platforms.manage", "providers.read",
            "storage.read", "storage.manage", "reports.read", "health.read", "logs.read", "settings.read"
        ]),
        new("Support", "Customer account and media support.",
        [
            "dashboard.read", "users.read", "users.manage",
            "media.read", "media.manage", "jobs.read", "jobs.manage", "logs.read", "reports.read"
        ]),
        new("Technical", "Technical monitoring and provider visibility.",
        [
            "dashboard.read", "users.read", "audit.read",
            "media.read", "jobs.read", "providers.read", "storage.read",
            "health.read", "logs.read", "reports.read", "settings.read"
        ]),
        new("Analyst", "Read-only reporting access.",
        [
            "dashboard.read", "users.read", "media.read", "jobs.read",
            "reports.read", "reports.export", "platforms.read", "health.read"
        ])
    ];
}
