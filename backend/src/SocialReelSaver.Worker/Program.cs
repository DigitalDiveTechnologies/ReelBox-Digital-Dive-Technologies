using Microsoft.Extensions.Options;
using Serilog;
using SocialReelSaver.Application;
using SocialReelSaver.Infrastructure;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Infrastructure.Workers;
using SocialReelSaver.Shared.Configuration;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.local.json",
        optional: true,
        reloadOnChange: true);

    // Native Windows Service when started by SCM; still runs as console in Development.
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ReelBox Download Worker";
    });

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SocialReelSaver.Worker")
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    // Sole Production consumer when API has Worker:RunInApiHost=false.
    builder.Services.AddHostedService<MediaDownloadWorker>();

    var host = builder.Build();
    var storageRoot = LocalStoragePath.Resolve(
        host.Services.GetRequiredService<IOptions<ObjectStorageOptions>>().Value.LocalRootPath);
    Directory.CreateDirectory(storageRoot);
    Log.Information(
        "SocialReelSaver.Worker started as dedicated download consumer (storage root {StorageRoot})",
        storageRoot);
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
