using System.Threading.Channels;

namespace Core;

/// <summary>
/// In-memory <see cref="IJobQueue"/> backed by an unbounded
/// <see cref="Channel{T}"/>. Safe for concurrent producers and consumers.
/// </summary>
public sealed class JobQueue : IJobQueue
{
    private readonly Channel<BackgroundJob> _channel;

    public JobQueue()
    {
        // Unbounded: producers never block. The channel itself is thread-safe
        // for multiple readers and writers.
        var options = new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateUnbounded<BackgroundJob>(options);
    }

    public ValueTask EnqueueAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<BackgroundJob> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
