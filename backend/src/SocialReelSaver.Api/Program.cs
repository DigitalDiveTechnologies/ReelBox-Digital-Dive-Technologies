using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using SocialReelSaver.Api.Extensions;
using SocialReelSaver.Api.Middleware;
using SocialReelSaver.Application;
using SocialReelSaver.Infrastructure;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Shared.Configuration;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.local.json",
        optional: true,
        reloadOnChange: true);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SocialReelSaver.Api")
        .WriteTo.Console());

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // SmarterASP / reverse proxies — clear defaults so X-Forwarded-* is honored.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);

    // In-process download consumer: Development convenience only.
    // Production VPS must run SocialReelSaver.Worker as the sole Redis consumer
    // (Worker:RunInApiHost defaults to true in Development, false otherwise).
    var runInApiHost = builder.Configuration.GetValue(
        "Worker:RunInApiHost",
        defaultValue: builder.Environment.IsDevelopment());
    if (runInApiHost)
    {
        builder.Services.AddHostedService<SocialReelSaver.Infrastructure.Workers.MediaDownloadWorker>();
    }

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    var corsOrigins = builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AdminPanel", policy =>
        {
            if (corsOrigins.Length == 0)
            {
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    if (runInApiHost)
    {
        Log.Information("API host registered MediaDownloadWorker (Worker:RunInApiHost=true)");
    }
    else
    {
        Log.Information(
            "API host will not consume download jobs; use SocialReelSaver.Worker (Worker:RunInApiHost=false)");
    }

    {
        var redis = app.Configuration["Redis:ConnectionString"]
            ?? app.Configuration.GetConnectionString("Redis")
            ?? string.Empty;
        var useInMemory = app.Configuration.GetValue("Worker:UseInMemoryQueue", false);
        var queueName = app.Configuration["Worker:QueueName"] ?? "media-download-jobs";
        Log.Information(
            "Download job queue: UseInMemory={UseInMemory} RedisConfigured={RedisConfigured} QueueName={QueueName}",
            useInMemory,
            !string.IsNullOrWhiteSpace(redis),
            queueName);
    }

    // SMTP startup diagnostics (never log password values).
    {
        var smtp = app.Configuration.GetSection("Smtp");
        var host = smtp["Host"] ?? string.Empty;
        var port = smtp["Port"] ?? string.Empty;
        var sslRaw = smtp["EnableSsl"] ?? string.Empty;
        var username = smtp["Username"] ?? string.Empty;
        var password = smtp["Password"] ?? string.Empty;
        var fromEmail = smtp["FromEmail"] ?? string.Empty;
        Log.Information(
            "SMTP diagnostics: Host={SmtpHost} Port={SmtpPort} EnableSsl={SmtpSsl} FromEmailConfigured={FromConfigured} UsernameConfigured={UserConfigured} PasswordConfigured={PassConfigured}",
            host,
            port,
            sslRaw,
            !string.IsNullOrWhiteSpace(fromEmail),
            !string.IsNullOrWhiteSpace(username),
            !string.IsNullOrWhiteSpace(password));
    }

    app.UseForwardedHeaders();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    // Shared local media root (videos + thumbnails). Same path as Worker ObjectStorage/Storage config.
    {
        var storageOptions = app.Services.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
        var storageRoot = LocalStoragePath.Resolve(storageOptions.LocalRootPath);
        Directory.CreateDirectory(storageRoot);
        Log.Information("Local media storage root: {StorageRoot}", storageRoot);

        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".mp4"] = "video/mp4";
        contentTypes.Mappings[".webm"] = "video/webm";
        contentTypes.Mappings[".mov"] = "video/quicktime";
        contentTypes.Mappings[".jpg"] = "image/jpeg";
        contentTypes.Mappings[".jpeg"] = "image/jpeg";
        contentTypes.Mappings[".png"] = "image/png";
        contentTypes.Mappings[".webp"] = "image/webp";

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storageRoot),
            RequestPath = "/storage",
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = false,
            // Android / browsers need byte-range support for progressive MP4 playback.
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.AcceptRanges = "bytes";
            },
        });
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors("AdminPanel");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    }).AllowAnonymous();
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false,
    }).AllowAnonymous();

    app.MapControllers();

    await SocialReelSaver.Infrastructure.Persistence.AdminUserBootstrap.EnsureSeedAsync(app.Services);

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
