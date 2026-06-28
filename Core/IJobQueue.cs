namespace Core;

/// <summary>
/// Thread-safe queue for handing <see cref="BackgroundJob"/> items from
/// producers (e.g. the Api) to consumers (e.g. the Worker).
/// </summary>
public interface IJobQueue
{
    /// <summary>Adds a job to the queue.</summary>
    ValueTask EnqueueAsync(BackgroundJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and returns the next job, asynchronously waiting until one is
    /// available or the token is cancelled.
    /// </summary>
    ValueTask<BackgroundJob> DequeueAsync(CancellationToken cancellationToken = default);
}
