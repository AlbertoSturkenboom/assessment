namespace Core;

/// <summary>
/// Represents the lifecycle state of a <see cref="BackgroundJob"/>.
/// </summary>
public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
