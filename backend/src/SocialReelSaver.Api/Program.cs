using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using SocialReelSaver.Api.Extensions;
using SocialReelSaver.Api.Middleware;
using SocialReelSaver.Application;
using SocialReelSaver.Infrastructure;

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

    // Process Queued download jobs in-process (yt-dlp → Downloading → Completed).
    builder.Services.AddHostedService<SocialReelSaver.Infrastructure.Workers.MediaDownloadWorker>();

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
