namespace Core;

/// <summary>
/// A unit of work that can be queued and processed by the Worker.
/// </summary>
public class BackgroundJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
