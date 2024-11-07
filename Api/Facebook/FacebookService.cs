using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Api.Facebook;

public interface IFacebookService
{
    public Task<FacebookGroup?> GetGroupAsync(string groupId);
}

class FacebookErrorResponse
{
    public required string Message { get; set; }
    public required int Code { get; set; }
}

class FacebookResponse
{
    public required FacebookErrorResponse Error { get; set; }
}

public class FacebookService(IHttpClientFactory httpClientFactory, IOptions<FacebookOptions> options, IMemoryCache cache)
    : IFacebookService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Facebook");
    private readonly FacebookOptions _options = options.Value;
    private static readonly SemaphoreSlim GroupLock = new(1, 1);

    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
    };

    async Task<FacebookGroup?> IFacebookService.GetGroupAsync(string groupId)
    {
        await GroupLock.WaitAsync();
        try
        {
            var url = $"{groupId}?fields=description,cover";

            try
            {
                var cacheKey = $"Group-{groupId}";
                var group = cache.Get<FacebookGroup>(cacheKey) ?? await _httpClient.GetFromJsonAsync<FacebookGroup>(url);

                if (group is not null)
                {
                    cache.Set(cacheKey, group, _cacheOptions);
                }

                return group;
            }
            catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.BadRequest)
            {
                var response = await _httpClient.GetAsync(url);
                var error = await response.Content.ReadFromJsonAsync<FacebookResponse>();
                if (error!.Error.Code == 100) return null;

                throw;
            }
        }
        finally
        {
            GroupLock.Release();
        }
    }
}
