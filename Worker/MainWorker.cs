using Core;

namespace Worker;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker> _logger;
    private readonly Greeter _greeter;

    public MainWorker(ILogger<MainWorker> logger, Greeter greeter)
    {
        _logger = logger;
        _greeter = greeter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("{greeting} Worker running at: {time}",
                    _greeter.Greet("Worker"), DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
