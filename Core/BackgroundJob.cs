namespace Core;

/// <summary>
/// A unit of work that can be queued and processed by the Worker.
/// </summary>
/// <remarks>
/// A job instance is shared by reference between the producer (Api) and the
/// consumer (Worker). Only <see cref="Status"/> changes after the job has been
/// published to the queue/store, so it is read and written with volatile
/// semantics to guarantee the Api always observes the latest value written by
/// the Worker. The other properties are set once before publication and are not
/// mutated afterwards.
/// </remarks>
public class BackgroundJob
{
    private int _status = (int)JobStatus.Pending;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public JobStatus Status
    {
        get => (JobStatus)Volatile.Read(ref _status);
        set => Volatile.Write(ref _status, (int)value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
