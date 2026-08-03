using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Application.AdminAuth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.AdminAuth.UseCases;

public sealed class GetCurrentAdminUseCase
{
    private readonly IAdminUserRepository _admins;

    public GetCurrentAdminUseCase(IAdminUserRepository admins)
    {
        _admins = admins;
    }

    public async Task<AdminProfileResponse> HandleAsync(
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await _admins.GetByIdAsync(adminId, cancellationToken);
        if (admin is null || !admin.IsActive)
        {
            throw new NotFoundException("Administrator was not found.");
        }

        return admin.ToProfileResponse();
    }
}
