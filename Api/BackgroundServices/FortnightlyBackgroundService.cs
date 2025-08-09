namespace Api.BackgroundServices;

public class FortnightlyBackgroundService(ILogger<FortnightlyBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _delay = TimeSpan.FromDays(14); // Run every fortnight

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Fortnightly background task starting at: {time}", DateTimeOffset.Now);

                await DoFortnightlyWork(stoppingToken);

                logger.LogInformation("Fortnightly background task completed at: {time}", DateTimeOffset.Now);

                await Task.Delay(_delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Fortnightly background task was cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in the fortnightly background task.");
            throw;
        }
    }

    private async Task DoFortnightlyWork(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fortnightly background service is starting.");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fortnightly background service is stopping.");
        await base.StopAsync(cancellationToken);
    }
}
