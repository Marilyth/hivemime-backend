public abstract class BaseWorker : BackgroundService
{
    protected readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly int _intervalInMinutes;
    private bool _isRunning;

    public BaseWorker(int intervalInMinutes, IServiceScopeFactory serviceScopeFactory, ILogger logger)
    {
        _intervalInMinutes = intervalInMinutes;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_intervalInMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_isRunning)
            {
                _logger.LogWarning("Previous execution is still running. Skipping this cycle.");
                continue;
            }
            
            _isRunning = true;

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HiveMimeContext>();
                await DoWorkAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating hotness.");
            }
            finally
            {
                _isRunning = false;
            }
        }
    }

    protected abstract Task DoWorkAsync(HiveMimeContext context);
}