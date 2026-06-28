using Core;

namespace Worker;

/// <summary>
/// Consumes jobs from the shared <see cref="IJobQueue"/> and updates their
/// status in the shared <see cref="IJobStore"/>.
/// </summary>
public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker> _logger;
    private readonly IJobQueue _queue;
    private readonly IJobStore _store;

    public MainWorker(
        ILogger<MainWorker> logger,
        IJobQueue queue,
        IJobStore store)
    {
        _logger = logger;
        _queue = queue;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started, waiting for jobs...");

        while (!stoppingToken.IsCancellationRequested)
        {
            BackgroundJob job;
            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
                break;
            }

            await ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(BackgroundJob job, CancellationToken stoppingToken)
    {
        try
        {
            job.Status = JobStatus.Processing;
            _store.Save(job);
            _logger.LogInformation("Processing job {JobId} ({Title})", job.Id, job.Title);

            // Simulate doing work for 3 seconds.
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            job.Status = JobStatus.Completed;
            _logger.LogInformation("Completed job {JobId} ({Title})", job.Id, job.Title);
        }
        catch (OperationCanceledException)
        {
            // Shutting down mid-job; leave it as Processing.
            throw;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
        }
        finally
        {
            // Keep the shared store in sync with the final status.
            _store.Save(job);
        }
    }
}
