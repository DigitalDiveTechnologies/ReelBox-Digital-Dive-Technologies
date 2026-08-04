using System.Threading.Channels;
using SocialReelSaver.Application.Abstractions.Media;

namespace SocialReelSaver.Infrastructure.Media;

public sealed class MediaCategorizationQueue : IMediaCategorizationQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

    public ValueTask EnqueueAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(mediaId, cancellationToken);

    public async ValueTask<Guid?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (_channel.Reader.TryRead(out var id))
        {
            return id;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
