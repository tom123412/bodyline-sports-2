using System;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Web.Facebook;

public class FacebookApiClient(HttpClient httpClient, IOptions<FacebookOptions> options)
{
    private readonly FacebookOptions _options = options.Value;

    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }

    public async Task<FacebookGroup?> GetGroup(CancellationToken cancellationToken = default)
    {
        var group = await httpClient.GetFromJsonAsync<FacebookGroup>($"/groups/{_options.GroupId}", cancellationToken: cancellationToken);
        return group;
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
