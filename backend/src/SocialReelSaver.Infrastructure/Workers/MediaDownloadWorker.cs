using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Workers;

/// <summary>
/// Background consumer for media download jobs (SRS §11 / §19).
/// Hosted by the Worker process and/or in-process inside the API (RapidAPI path).
/// </summary>
public sealed class MediaDownloadWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkerOptions> _options;
    private readonly ILogger<MediaDownloadWorker> _logger;

    public MediaDownloadWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<MediaDownloadWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Media download worker started. Queue={QueueName}",
            _options.Value.QueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<IMediaJobConsumer>();
                var pipeline = scope.ServiceProvider.GetRequiredService<MediaDownloadPipeline>();

                var job = await consumer.ConsumeAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(_options.Value.PollIntervalMilliseconds, stoppingToken);
                    continue;
                }

                _logger.LogInformation(
                    "Processing job {JobId} for media {MediaId} (attempt {Attempt})",
                    job.JobId,
                    job.MediaId,
                    job.Attempt);

                await pipeline.ExecuteAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(_options.Value.PollIntervalMilliseconds, stoppingToken);
            }
        }

        _logger.LogInformation("Media download worker stopped");
    }
}
