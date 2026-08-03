using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Abstractions.Admin;

public interface IAdminHealthProbe
{
    Task<SystemHealthOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<HealthComponentStatus> ProbeProviderAsync(string name, CancellationToken cancellationToken = default);
}
