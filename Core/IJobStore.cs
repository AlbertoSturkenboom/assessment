using System.Diagnostics.CodeAnalysis;

namespace Core;

/// <summary>
/// Stores submitted jobs so their current state can be looked up by id.
/// Abstracts away the underlying storage (currently in-memory).
/// </summary>
public interface IJobStore
{
    /// <summary>Adds or updates a job.</summary>
    void Save(BackgroundJob job);

    /// <summary>Returns the job with the given id, if present.</summary>
    bool TryGet(Guid id, [MaybeNullWhen(false)] out BackgroundJob job);
}
