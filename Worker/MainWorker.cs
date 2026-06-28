using System.Collections.Concurrent;
using Core;

namespace Worker;

/// <summary>
/// Consumes jobs from the shared <see cref="IJobQueue"/> and updates their
/// status in the shared in-memory store.
/// </summary>
public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker> _logger;
    private readonly IJobQueue _queue;
    private readonly ConcurrentDictionary<Guid, BackgroundJob> _store;

    public MainWorker(
        ILogger<MainWorker> logger,
        IJobQueue queue,
        ConcurrentDictionary<Guid, BackgroundJob> store)
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
            _logger.LogInformation("Processing job {JobId} ({Title})", job.Id, job.Title);

            // Simulate work.
            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);

            job.Status = JobStatus.Completed;
            _logger.LogInformation("Completed job {JobId}", job.Id);
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
            // Keep the shared store in sync (the job is the same reference the
            // Api stored, but this is explicit and safe either way).
            _store[job.Id] = job;
        }
    }
}
