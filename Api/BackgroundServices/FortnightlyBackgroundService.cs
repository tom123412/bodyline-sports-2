using Api.Azure;
using Api.Facebook;
using Microsoft.Extensions.Options;

namespace Api.BackgroundServices;

public class FortnightlyBackgroundService(ILogger<FortnightlyBackgroundService> logger, IOptionsMonitor<FacebookOptions> options, IServiceScopeFactory serviceScopeFactory) : BackgroundService
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
        using var scope = serviceScopeFactory.CreateScope();
        var facebookService = scope.ServiceProvider.GetRequiredService<IFacebookService>();
        var azureService = scope.ServiceProvider.GetRequiredService<IAzureService>();

        var newAccessToken = await facebookService.RefreshAccessTokenAsync(options.CurrentValue.AccessToken);
        await azureService.SaveConfigurationSettingsAsync($"{nameof(FacebookOptions)}:{nameof(FacebookOptions.AccessToken)}", newAccessToken);
        logger.LogInformation("access token lifetime extended till {expiry}", (await facebookService.GetLongLivedTokenDetailsAsync(newAccessToken)).Data.ExpiresAt);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }
}
