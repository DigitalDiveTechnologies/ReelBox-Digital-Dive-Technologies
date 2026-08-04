using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Admin.UseCases;
using SocialReelSaver.Application.AdminAuth.UseCases;
using SocialReelSaver.Application.Auth.UseCases;
using SocialReelSaver.Application.Media.Services;
using SocialReelSaver.Application.Media.UseCases;

namespace SocialReelSaver.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserUseCase>();

        services.AddSingleton<IMediaUrlAnalyzer, MediaUrlAnalyzer>();

        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUserUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();
        services.AddScoped<ForgotUserPasswordUseCase>();
        services.AddScoped<ResetUserPasswordUseCase>();
        services.AddScoped<VerifySignupOtpUseCase>();
        services.AddScoped<ResendSignupOtpUseCase>();

        services.AddScoped<LoginAdminUseCase>();
        services.AddScoped<RefreshAdminTokenUseCase>();
        services.AddScoped<LogoutAdminUseCase>();
        services.AddScoped<GetCurrentAdminUseCase>();
        services.AddScoped<ForgotAdminPasswordUseCase>();
        services.AddScoped<ResetAdminPasswordUseCase>();

        services.AddScoped<GetDashboardSummaryUseCase>();
        services.AddScoped<GetDashboardTrendsUseCase>();
        services.AddScoped<GetDashboardActivityUseCase>();
        services.AddScoped<ListUsersAdminUseCase>();
        services.AddScoped<GetUserAdminUseCase>();
        services.AddScoped<UpdateUserStatusUseCase>();
        services.AddScoped<RevokeUserSessionsUseCase>();
        services.AddScoped<ListAdminAccountsUseCase>();
        services.AddScoped<GetAdminAccountUseCase>();
        services.AddScoped<CreateAdminAccountUseCase>();
        services.AddScoped<UpdateAdminAccountUseCase>();
        services.AddScoped<AssignAdminRoleUseCase>();
        services.AddScoped<ListRolesUseCase>();
        services.AddScoped<ListAuditLogsUseCase>();
        services.AddScoped<GetAuditLogUseCase>();

        services.AddScoped<ListMediaAdminUseCase>();
        services.AddScoped<GetMediaAdminUseCase>();
        services.AddScoped<DeleteMediaAdminUseCase>();
        services.AddScoped<RetryMediaAdminUseCase>();
        services.AddScoped<GetMediaPlaybackAdminUseCase>();
        services.AddScoped<ListJobsAdminUseCase>();
        services.AddScoped<RetryJobAdminUseCase>();
        services.AddScoped<CancelJobAdminUseCase>();
        services.AddScoped<RequeueJobAdminUseCase>();
        services.AddScoped<ListPlatformsAdminUseCase>();
        services.AddScoped<UpdatePlatformAdminUseCase>();
        services.AddScoped<ListProvidersAdminUseCase>();
        services.AddScoped<UpdateProviderAdminUseCase>();
        services.AddScoped<ProbeProviderHealthUseCase>();
        services.AddScoped<GetStorageSummaryUseCase>();
        services.AddScoped<ScanStorageOrphansUseCase>();
        services.AddScoped<CleanupStorageOrphansUseCase>();
        services.AddScoped<GetDownloadsTrendsUseCase>();
        services.AddScoped<GetUserActivityReportUseCase>();
        services.AddScoped<GetPlatformStatsUseCase>();
        services.AddScoped<GetProviderPerformanceUseCase>();
        services.AddScoped<ExportReportCsvUseCase>();
        services.AddScoped<GetSystemHealthOverviewUseCase>();
        services.AddScoped<ListAppErrorLogsUseCase>();
        services.AddScoped<GetAppErrorLogUseCase>();
        services.AddScoped<GetSettingsAdminUseCase>();
        services.AddScoped<UpsertSettingsAdminUseCase>();

        services.AddScoped<CreateMediaUseCase>();
        services.AddScoped<GetMediaListUseCase>();
        services.AddScoped<GetMediaByIdUseCase>();
        services.AddScoped<RetryMediaUseCase>();
        services.AddScoped<DeleteMediaUseCase>();
        services.AddScoped<GetPlaybackUseCase>();
        services.AddScoped<GetMediaContentUseCase>();

        return services;
    }
}
