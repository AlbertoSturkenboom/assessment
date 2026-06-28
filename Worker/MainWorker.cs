using Core;

namespace Worker;

/// <summary>
/// Consumes jobs from the shared <see cref="IJobQueue"/> and updates their
/// status in the shared <see cref="IJobStore"/>. Runs several consumer loops
/// so jobs can be processed in parallel.
/// </summary>
public sealed class MainWorker(
    ILogger<MainWorker> logger,
    IJobQueue queue,
    IJobStore store,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _maxConcurrency =
        Math.Max(1, configuration.GetValue("Worker:MaxConcurrency", 4));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so host startup is never blocked by anything that
        // runs before the first real await, regardless of future changes here.
        await Task.Yield();

        logger.LogInformation(
            "Worker started with concurrency {Concurrency}, waiting for jobs...",
            _maxConcurrency);

        var consumers = Enumerable.Range(0, _maxConcurrency)
            .Select(_ => ConsumeAsync(stoppingToken));

        await Task.WhenAll(consumers);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            BackgroundJob job;
            try
            {
                job = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(BackgroundJob job, CancellationToken stoppingToken)
    {
        try
        {
            store.Save(job.MarkProcessing());
            logger.LogInformation("Processing job {JobId} ({Title})", job.Id, job.Title);

            var result = await PerformWorkAsync(job, stoppingToken);

            store.Save(job.MarkCompleted(result));
            logger.LogInformation("Completed job {JobId} ({Title})", job.Id, job.Title);
        }
        catch (OperationCanceledException)
        {
            // Shutting down mid-job; leave the last saved (Processing) snapshot.
        }
        catch (Exception ex)
        {
            store.Save(job.MarkFailed(ex.Message));
            logger.LogError(ex, "Job {JobId} failed", job.Id);
        }
    }

    /// <summary>The actual unit of work; the obvious place to plug in real logic.</summary>
    private static async Task<string> PerformWorkAsync(BackgroundJob job, CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        return $"Processed '{job.Title}'";
    }
}
