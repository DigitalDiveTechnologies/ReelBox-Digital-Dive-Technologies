namespace SocialReelSaver.Application.Admin.DTOs;

public sealed record RoleDefinitionResponse(string Name, string Description, IReadOnlyList<string> Permissions);

public sealed record RolesListResponse(IReadOnlyList<RoleDefinitionResponse> Items);
