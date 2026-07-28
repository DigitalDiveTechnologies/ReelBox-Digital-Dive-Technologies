using Serilog;
using SocialReelSaver.Application;
using SocialReelSaver.Infrastructure;
using SocialReelSaver.Infrastructure.Workers;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SocialReelSaver.Worker")
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<MediaDownloadWorker>();

    var host = builder.Build();
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
