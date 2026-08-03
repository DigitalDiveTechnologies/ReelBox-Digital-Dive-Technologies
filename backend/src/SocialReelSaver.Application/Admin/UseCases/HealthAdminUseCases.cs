using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class GetSystemHealthOverviewUseCase(IAdminHealthProbe probe)
{
    public Task<SystemHealthOverviewResponse> HandleAsync(CancellationToken cancellationToken = default) =>
        probe.GetOverviewAsync(cancellationToken);
}

public sealed class ProbeProviderHealthUseCase(IAdminHealthProbe probe)
{
    public Task<HealthComponentStatus> HandleAsync(string name, CancellationToken cancellationToken = default) =>
        probe.ProbeProviderAsync(name, cancellationToken);
}
