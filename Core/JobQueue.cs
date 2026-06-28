using System.Threading.Channels;

namespace Core;

/// <summary>
/// In-memory <see cref="IJobQueue"/> backed by an unbounded
/// <see cref="Channel{T}"/>. Safe for concurrent producers and consumers.
/// </summary>
public sealed class JobQueue : IJobQueue
{
    // Unbounded: producers never block. Defaults already allow multiple
    // concurrent readers and writers.
    private readonly Channel<BackgroundJob> _channel = Channel.CreateUnbounded<BackgroundJob>();

    public ValueTask EnqueueAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<BackgroundJob> DequeueAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);
}
