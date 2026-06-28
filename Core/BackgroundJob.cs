namespace Core;

/// <summary>
/// A unit of work that can be queued and processed by the Worker.
/// </summary>
/// <remarks>
/// Immutable: state transitions produce a new snapshot (via <c>with</c>) that
/// replaces the previous one in the store. Because each snapshot is fully
/// constructed before it is published and the store swaps the reference
/// atomically, a reader (the Api) always observes an internally consistent set
/// of fields — e.g. it can never see <see cref="JobStatus.Completed"/> without
/// the matching <see cref="CompletedAt"/>/<see cref="Result"/>.
/// </remarks>
public record BackgroundJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Title { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public JobStatus Status { get; init; } = JobStatus.Pending;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Set together with a terminal status (Completed or Failed).</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Result payload, set when the job completes successfully.</summary>
    public string? Result { get; init; }

    /// <summary>Error description, set when the job fails.</summary>
    public string? ErrorMessage { get; init; }
}
