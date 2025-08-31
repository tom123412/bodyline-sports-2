using Api.Azure;
using Api.Facebook;
using Microsoft.Extensions.Options;

namespace Api.BackgroundServices;

public class FortnightlyBackgroundService(
    ILogger<FortnightlyBackgroundService> logger,
    IOptionsMonitor<FacebookOptions> options,
    IOptionsMonitor<BackgroundServiceOptions> backgroundOptions,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
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
            //don't throw as it causes the whole app to shut down
        }
    }

    private async Task DoFortnightlyWork(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var facebookService = scope.ServiceProvider.GetRequiredService<IFacebookService>();

        logger.LogDebug("AppId is {appId}", options.CurrentValue.AppId);
        logger.LogDebug("Checking Facebook access token expiry {token}...", options.CurrentValue.AccessToken[..10]);
        var tokenDetails = await facebookService.GetLongLivedTokenDetailsAsync(options.CurrentValue.AccessToken);
        var expiresAt = tokenDetails.Data.ExpiresAt;
        var timeUntilExpiration = expiresAt - DateTimeOffset.UtcNow;
        var refreshThreshold = TimeSpan.FromDays(backgroundOptions.CurrentValue.TokenRefreshThresholdDays);

        if (timeUntilExpiration <= refreshThreshold)
        {
            logger.LogInformation("Token will expire in {ExpiresIn}. Refreshing token...", timeUntilExpiration);
            var newAccessToken = await facebookService.RefreshAccessTokenAsync(options.CurrentValue.AccessToken);

            var azureService = scope.ServiceProvider.GetRequiredService<IAzureService>();
            await azureService.SaveConfigurationSettingsAsync($"{nameof(FacebookOptions)}:{nameof(FacebookOptions.AccessToken)}", newAccessToken);

            var newTokenDetails = await facebookService.GetLongLivedTokenDetailsAsync(newAccessToken);
            logger.LogInformation("Access token lifetime extended till {expiry:o}", newTokenDetails.Data.ExpiresAt);
        }
        else
        {
            logger.LogInformation("Token is still valid for {TimeRemaining}. No update needed.", timeUntilExpiration);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }
}
