using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Api.Facebook;

public interface IFacebookService
{
    public Task<FacebookGroup?> GetGroupAsync(string groupId);
    public Task<IEnumerable<FacebookPost>> GetPostsForGroupAsync(string groupId, int? limit);
}

public class FacebookService(IHttpClientFactory httpClientFactory, IOptions<FacebookOptions> options, IMemoryCache cache)
    : IFacebookService
{

    private class FacebookErrorResponse
    {
        public required string Message { get; set; }
        public required int Code { get; set; }
    }

    private class FacebookResponse
    {
        public required FacebookErrorResponse Error { get; set; }
    }

    private class FacebookGroupFeed
    {
        public required FacebookPost[] Data { get; set; }
    }

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Facebook");
    private readonly FacebookOptions _options = options.Value;

    private static readonly SemaphoreSlim GroupLock = new(1, 1);
    private static readonly SemaphoreSlim PostsLock = new(1, 1);

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

    async Task<IEnumerable<FacebookPost>> IFacebookService.GetPostsForGroupAsync(string groupId, int? limit)
    {
        await PostsLock.WaitAsync();
        try
        {
            var cacheKey = $"Posts-{groupId}";
            var posts = cache.Get<FacebookPost[]>(cacheKey)?.ToList() ?? [];
            var url = $"/{groupId}/feed?fields=attachments,message,updated_time&since={posts.FirstOrDefault()?.UpdatedDateTime.ToString("s")}&limit={limit ?? _options.PostsToLoad}";
            var feed = await _httpClient.GetFromJsonAsync<FacebookGroupFeed>(url);
            var newPosts = (feed?.Data ?? []).ToList();

            newPosts.AddRange(posts.Where(p => p.Type != "Status"));

            cache.Set(cacheKey, newPosts.ToArray(), _cacheOptions);

            return cache.Get<IEnumerable<FacebookPost>>(cacheKey)!.Take(limit ?? _options.PostsToLoad);
        }
        finally
        {
            PostsLock.Release();
        }
    }
}
