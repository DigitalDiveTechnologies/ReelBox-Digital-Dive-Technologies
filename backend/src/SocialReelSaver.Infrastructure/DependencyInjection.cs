using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Application.Abstractions.Email;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Media.Retry;
using SocialReelSaver.Infrastructure.Admin;
using SocialReelSaver.Infrastructure.Authentication;
using SocialReelSaver.Infrastructure.Downloading;
using SocialReelSaver.Infrastructure.Media;
using SocialReelSaver.Infrastructure.Persistence;
using SocialReelSaver.Infrastructure.Persistence.Repositories;
using SocialReelSaver.Infrastructure.Playback;
using SocialReelSaver.Infrastructure.Providers;
using SocialReelSaver.Infrastructure.Queue;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Infrastructure.Email;
using SocialReelSaver.Infrastructure.Workers;
using SocialReelSaver.Shared.Configuration;
using StackExchange.Redis;

namespace SocialReelSaver.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));
        services.Configure<DownloadOptions>(configuration.GetSection(DownloadOptions.SectionName));
        services.Configure<ProvidersOptions>(configuration.GetSection(ProvidersOptions.SectionName));
        services.Configure<RapidApiOptions>(configuration.GetSection(RapidApiOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminJwtOptions>(configuration.GetSection(AdminJwtOptions.SectionName));
        services.Configure<AdminBootstrapOptions>(configuration.GetSection(AdminBootstrapOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));
        services.Configure<FfmpegOptions>(configuration.GetSection(FfmpegOptions.SectionName));

        var databaseConnection = configuration.GetSection(DatabaseOptions.SectionName)["ConnectionString"]
            ?? configuration.GetConnectionString("PostgreSQL")
            ?? string.Empty;

        var redisConnection = configuration.GetSection(RedisOptions.SectionName)["ConnectionString"]
            ?? configuration.GetConnectionString("Redis")
            ?? string.Empty;

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                databaseConnection,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IAdminMetricsReader, AdminMetricsReader>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IAppErrorLogRepository, AppErrorLogRepository>();
        services.AddScoped<IAppErrorLogWriter, AppErrorLogWriter>();
        services.AddSingleton<IOperationalSettings, OperationalSettings>();
        services.AddScoped<IAdminStorageScanner, AdminStorageScanner>();
        services.AddScoped<IAdminHealthProbe, AdminHealthProbe>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IAdminJwtTokenService, AdminJwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAdminRefreshTokenService, AdminRefreshTokenService>();
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<IAdminAuthenticationService, AdminAuthenticationService>();
        services.AddSingleton<IEmailService, SmtpEmailService>();

        services.AddScoped<IMediaStatusService, MediaStatusService>();
        services.AddSingleton<IRetryPolicy, ExponentialBackoffRetryPolicy>();
        services.AddScoped<MediaDownloadPipeline>();

        services.AddHttpClient(MetaGraphMediaResolver.HttpClientName, (sp, client) =>
        {
            var timeoutSeconds = configuration.GetValue("Providers:TimeoutSeconds", 30);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds) + 5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SocialReelSaver/1.0");
        });
        services.AddHttpClient(RapidApiMediaResolver.HttpClientName, (sp, client) =>
        {
            var timeoutSeconds = configuration.GetValue("Providers:TimeoutSeconds", 30);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds) + 5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SocialReelSaver/1.0");
        });
        services.AddSingleton<MetaGraphMediaResolver>();
        services.AddSingleton<YtDlpMediaResolver>();
        services.AddSingleton<RapidApiMediaResolver>();
        services.AddSingleton<IMediaProvider, InstagramProvider>();
        services.AddSingleton<IMediaProvider, FacebookProvider>();
        services.AddSingleton<IMediaProviderFactory, MediaProviderFactory>();
        services.AddSingleton<IMediaProviderResolver, MediaProviderResolver>();
        services.AddSingleton<IProviderResultValidator, ProviderResultValidator>();
        services.AddSingleton<IMediaProviderExecutor, MediaProviderExecutor>();

        services.AddSingleton<ITemporaryFileManager, TemporaryFileManager>();
        services.AddSingleton<IDownloadValidator, DownloadValidator>();
        services.AddSingleton<IThumbnailService, ThumbnailGenerator>();

        services.AddSingleton<LocalObjectStorageService>();
        services.AddSingleton<S3CompatibleObjectStorageService>();
        services.AddSingleton<CloudflareR2StorageService>();
        services.AddSingleton<IObjectStorageFactory, StorageFactory>();
        services.AddSingleton<IObjectStorageService>(sp => sp.GetRequiredService<IObjectStorageFactory>().Create());

        services.AddSingleton<LocalSignedUrlProvider>();
        services.AddSingleton<CloudSignedUrlProvider>();
        services.AddSingleton<IMediaThumbnailUrlService, MediaThumbnailUrlService>();
        services.AddSingleton<ISignedUrlProvider>(sp =>
        {
            var provider = sp.GetRequiredService<IOptions<ObjectStorageOptions>>().Value.Provider;
            return provider.Trim().Equals("Local", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<LocalSignedUrlProvider>()
                : sp.GetRequiredService<CloudSignedUrlProvider>();
        });
        services.AddSingleton<IPlaybackAuthorization, PlaybackAuthorization>();
        services.AddScoped<IPlaybackUrlService, PlaybackUrlService>();

        var downloadTimeoutSeconds = configuration.GetValue("Download:TimeoutSeconds", 120);
        services.AddHttpClient<IMediaDownloader, MediaDownloader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, downloadTimeoutSeconds) + 5);
            // Facebook/IG CDNs often reject bare clients; browser-like UA helps thumb fetches.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
        });

        RegisterQueue(services, configuration, redisConnection);

        services.AddScoped<IMediaJobPublisher, MediaJobPublisher>();
        services.AddScoped<IMediaJobConsumer, MediaJobConsumer>();

        var healthChecks = services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString: databaseConnection,
                name: "postgresql",
                tags: ["ready", "db"]);

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            healthChecks.AddRedis(
                redisConnectionString: redisConnection,
                name: "redis",
                tags: ["ready", "cache"]);
        }

        healthChecks.AddCheck<ObjectStorageHealthCheck>("object-storage", tags: ["ready", "storage"]);

        return services;
    }

    private static void RegisterQueue(
        IServiceCollection services,
        IConfiguration configuration,
        string redisConnection)
    {
        var useInMemory = configuration.GetValue("Worker:UseInMemoryQueue", false)
            || string.IsNullOrWhiteSpace(redisConnection);
        if (useInMemory)
        {
            services.AddSingleton<IMediaJobQueue, InMemoryMediaJobQueue>();
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IMediaJobQueue, RedisMediaJobQueue>();
    }
}
