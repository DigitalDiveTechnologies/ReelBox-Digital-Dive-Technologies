using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Shared.Configuration;
using StackExchange.Redis;

namespace SocialReelSaver.Infrastructure.Queue;

public sealed class MediaJobPublisher : IMediaJobPublisher
{
    private readonly IMediaJobQueue _queue;

    public MediaJobPublisher(IMediaJobQueue queue)
    {
        _queue = queue;
    }

    public Task PublishDownloadJobAsync(
        MediaDownloadJob job,
        CancellationToken cancellationToken = default) =>
        _queue.PublishAsync(job, cancellationToken);
}

public sealed class MediaJobConsumer : IMediaJobConsumer
{
    private readonly IMediaJobQueue _queue;

    public MediaJobConsumer(IMediaJobQueue queue)
    {
        _queue = queue;
    }

    public Task<MediaDownloadJob?> ConsumeAsync(CancellationToken cancellationToken = default) =>
        _queue.DequeueAsync(cancellationToken);
}

/// <summary>
/// In-process queue used for tests and local runs without Redis.
/// </summary>
public sealed class InMemoryMediaJobQueue : IMediaJobQueue
{
    private readonly Channel<MediaDownloadJob> _channel =
        Channel.CreateUnbounded<MediaDownloadJob>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

    public async Task PublishAsync(MediaDownloadJob job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async Task<MediaDownloadJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_channel.Reader.TryRead(out var job))
            {
                if (job.NotBefore is null || job.NotBefore <= DateTimeOffset.UtcNow)
                {
                    return job;
                }

                // Delayed retry — requeue and wait briefly.
                await _channel.Writer.WriteAsync(job, cancellationToken);
                await Task.Delay(200, cancellationToken);
                continue;
            }

            var wait = _channel.Reader.WaitToReadAsync(cancellationToken);
            if (!await wait)
            {
                return null;
            }
        }

        return null;
    }
}

/// <summary>
/// Redis list-based queue (SRS §18 MVP). Replaceable via <see cref="IMediaJobQueue"/>.
/// </summary>
public sealed class RedisMediaJobQueue : IMediaJobQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly WorkerOptions _options;

    public RedisMediaJobQueue(IConnectionMultiplexer redis, IOptions<WorkerOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task PublishAsync(MediaDownloadJob job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = _redis.GetDatabase();
        var payload = JsonSerializer.Serialize(job, JsonOptions);
        await db.ListLeftPushAsync(_options.QueueName, payload);
    }

    public async Task<MediaDownloadJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var timeout = TimeSpan.FromMilliseconds(Math.Max(100, _options.DequeueTimeoutMilliseconds));

        while (!cancellationToken.IsCancellationRequested)
        {
            var value = await db.ListRightPopAsync(_options.QueueName);
            if (value.IsNullOrEmpty)
            {
                await Task.Delay(timeout, cancellationToken);
                return null;
            }

            var job = JsonSerializer.Deserialize<MediaDownloadJob>(value!, JsonOptions);
            if (job is null)
            {
                continue;
            }

            if (job.NotBefore is not null && job.NotBefore > DateTimeOffset.UtcNow)
            {
                await db.ListLeftPushAsync(_options.QueueName, value);
                await Task.Delay(200, cancellationToken);
                return null;
            }

            return job;
        }

        return null;
    }
}
