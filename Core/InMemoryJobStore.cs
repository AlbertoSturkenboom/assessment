using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Core;

/// <summary>
/// Thread-safe in-memory <see cref="IJobStore"/> backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, BackgroundJob> _jobs = new();

    public void Save(BackgroundJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        _jobs[job.Id] = job;
    }

    public bool TryGet(Guid id, [MaybeNullWhen(false)] out BackgroundJob job)
        => _jobs.TryGetValue(id, out job);
}
