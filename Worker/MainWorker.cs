using Core;

namespace Worker;

/// <summary>
/// Consumes jobs from the shared <see cref="IJobQueue"/> and updates their
/// status in the shared <see cref="IJobStore"/>. Runs several consumer loops
/// so jobs can be processed in parallel.
/// </summary>
public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker> _logger;
    private readonly IJobQueue _queue;
    private readonly IJobStore _store;
    private readonly int _maxConcurrency;

    public MainWorker(
        ILogger<MainWorker> logger,
        IJobQueue queue,
        IJobStore store,
        IConfiguration configuration)
    {
        _logger = logger;
        _queue = queue;
        _store = store;
        _maxConcurrency = Math.Max(1, configuration.GetValue("Worker:MaxConcurrency", 4));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so host startup is never blocked by anything that
        // runs before the first real await, regardless of future changes here.
        await Task.Yield();

        _logger.LogInformation(
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
                job = await _queue.DequeueAsync(stoppingToken);
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
            _store.Save(job.MarkProcessing());
            _logger.LogInformation("Processing job {JobId} ({Title})", job.Id, job.Title);

            var result = await PerformWorkAsync(job, stoppingToken);

            _store.Save(job.MarkCompleted(result));
            _logger.LogInformation("Completed job {JobId} ({Title})", job.Id, job.Title);
        }
        catch (OperationCanceledException)
        {
            // Shutting down mid-job; leave the last saved (Processing) snapshot.
        }
        catch (Exception ex)
        {
            _store.Save(job.MarkFailed(ex.Message));
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
        }
    }

    /// <summary>The actual unit of work; the obvious place to plug in real logic.</summary>
    private static async Task<string> PerformWorkAsync(BackgroundJob job, CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        return $"Processed '{job.Title}'";
    }
}
