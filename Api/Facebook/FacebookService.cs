using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Api.Facebook.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Api.Facebook;

public interface IFacebookService
{
    public Task<FacebookGroup?> GetGroupAsync(string groupId, CancellationToken ct);
    public Task<IEnumerable<FacebookPost>> GetPostsForGroupAsync(string groupId, CancellationToken ct);
    public IAsyncEnumerable<FacebookPost> GetPostsForGroupAsync2(string groupId, CancellationToken ct);
    public Task<IEnumerable<FacebookPost>> GetPostsForPageAsync(string pageId, string accessToken, CancellationToken ct);
    public Task<FacebookTokenDetails> GetLongLivedTokenDetailsAsync(string userAccessToken, CancellationToken ct);
    public Task<FacebookTokenDetails> GetTokenDetailsAsync(string token, CancellationToken ct);
    public Task<string> RefreshAccessTokenAsync(string accessToken, CancellationToken ct);
}

public class FacebookService: IFacebookService
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

    private class FacebookFeed
    {
        public required FacebookPost[] Data { get; set; }
    }

    record FacebookCodeDetails(string Code);

    private static readonly SemaphoreSlim GroupLock = new(1, 1);
    private static readonly SemaphoreSlim PostsLock = new(1, 1);
    private readonly HttpClient _httpClient;
    private readonly ILogger<FacebookService> _logger;
    private readonly FacebookOptions _options;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public FacebookService(IHttpClientFactory httpClientFactory, 
        IOptionsSnapshot<FacebookOptions> options, ILogger<FacebookService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClientFactory.CreateClient("Facebook");
        _logger = logger;
        _options = options.Value;
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_options.HoursToCache),
        };
    }

    async Task<FacebookGroup?> IFacebookService.GetGroupAsync(string groupId, CancellationToken ct)
    {
        try
        {
            await GroupLock.WaitAsync(ct);
            var url = $"{groupId}?fields=description,cover";

            try
            {
                var cacheKey = $"Group-{groupId}";
                var group = _cache.Get<FacebookGroup>(cacheKey) ?? await _httpClient.GetFromJsonAsync<FacebookGroup>(url, ct);

                if (group is not null)
                {
                    _cache.Set(cacheKey, group, _cacheOptions);
                }

                return group;
            }
            catch (HttpRequestException hre) when (hre.StatusCode == HttpStatusCode.BadRequest)
            {
                var response = await _httpClient.GetAsync(url);
                var error = await response.Content.ReadFromJsonAsync<FacebookResponse>(ct);
                if (error!.Error.Code == 100) return null;

                throw;
            }
        }
        finally
        {
            GroupLock.Release();
        }
    }

    async Task<IEnumerable<FacebookPost>> IFacebookService.GetPostsForGroupAsync(string groupId, CancellationToken ct)
    {
        try
        {
            await PostsLock.WaitAsync(ct);
            var cacheKey = $"Posts-{groupId}";
            var posts = _cache.Get<FacebookPost[]>(cacheKey)?.ToList() ?? [];
            var url = $"/{groupId}/feed?fields=attachments,message,message_tags,updated_time&since={posts.FirstOrDefault()?.UpdatedDateTime.ToString("s")}&limit={ _options.PostsToLoad}";
            var feed = await _httpClient.GetFromJsonAsync<FacebookFeed>(url, ct);
            var newPosts = (feed?.Data ?? []).Where(p => !p.Tags.Where(t => _options.TagsToHide.Contains(t.Name)).Any()).ToList();

            newPosts.AddRange(posts.Where(p => p.Type != "Status"));

            _cache.Set(cacheKey, newPosts.ToArray(), _cacheOptions);

            return _cache.Get<IEnumerable<FacebookPost>>(cacheKey)!;
        }
        finally
        {
            PostsLock.Release();
        }
    }

    async Task<IEnumerable<FacebookPost>> IFacebookService.GetPostsForPageAsync(string pageId, string accessToken, CancellationToken ct)
    {
        var oneMonthAgo = DateTimeOffset.UtcNow.AddMonths(-1);
        var url = $"/{pageId}/feed?fields=attachments,message,message_tags,updated_time&since={oneMonthAgo.ToString("s")}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, ct);

        response.EnsureSuccessStatusCode();
        var feed = await response.Content.ReadFromJsonAsync<FacebookFeed>(ct);
        var newPosts = (feed?.Data ?? []).Where(p => !p.Tags.Any(t => _options.TagsToHide.Contains(t.Name))).ToList();

        return newPosts;
    }

    async Task<FacebookTokenDetails> IFacebookService.GetLongLivedTokenDetailsAsync(string userAccessToken, CancellationToken ct)
    {
        var url = $"/debug_token?input_token={_options.GroupAccessToken}&access_token={userAccessToken}";
        var tokenDetails = await _httpClient.GetFromJsonAsync<FacebookTokenDetails>(url, ct);
        return tokenDetails!;
    }

    async Task<string> IFacebookService.RefreshAccessTokenAsync(string accessToken, CancellationToken ct)
    {
        var url = $"oauth/access_token?grant_type=fb_exchange_token&client_id={_options.AppId}&client_secret={_options.AppSecret}&fb_exchange_token={accessToken}";
        var tokenDetails = await _httpClient.GetFromJsonAsync<FacebookRefreshedTokenDetails>(url, ct);
        // var codeUrl = $"oauth/client_code?client_id={_options.AppId}&client_secret={_options.AppSecret}&access_token={accessToken}";
        // var code = (await _httpClient.GetFromJsonAsync<FacebookCodeDetails>(codeUrl))!.Code;
        // var accessTokenUrl = $"oauth/access_token?code={code}&client_id={_options.AppId}";
        // var newAccessToken = (await _httpClient.GetFromJsonAsync<FacebookRefreshedTokenDetails>(accessTokenUrl))!.AccessToken;
        return tokenDetails!.AccessToken;
    }

    async IAsyncEnumerable<FacebookPost> IFacebookService.GetPostsForGroupAsync2(string groupId, [EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            await PostsLock.WaitAsync(ct);
            var cacheKey = $"Posts-{groupId}";
            var cachedPosts = _cache.Get<FacebookPost[]>(cacheKey)?.ToList() ?? [];
            var latestCachedPost = cachedPosts.OrderByDescending(p => p.UpdatedDateTime).FirstOrDefault();

            var latestCachedPostDate = latestCachedPost?.UpdatedDateTime ?? DateTimeOffset.UtcNow.AddYears(-1);
            DateTimeOffset? latestPostDate = null;
            
            List<FacebookPost> newPosts = [];
            var i = 0;
            for (; i < _options.PostsToLoad && !ct.IsCancellationRequested; i++)
            {
                var url = $"/{groupId}/feed?fields=attachments,message,message_tags,updated_time&until={latestPostDate?.ToString("s")}&limit=2";
                var feed = await _httpClient.GetFromJsonAsync<FacebookFeed>(url, ct);
                var sortedFeed = (feed?.Data ?? []).OrderByDescending(p => p.UpdatedDateTime).Where(p => !p.Tags.Any(t => _options.TagsToHide.Contains(t.Name)));
                var oldestPost = sortedFeed.LastOrDefault();
                
                if (oldestPost is null || cachedPosts.Any(p => p.Id == oldestPost.Id)) break;

                newPosts.AddRange(sortedFeed);

                foreach (var post in sortedFeed) yield return post;
                
                latestPostDate = oldestPost.UpdatedDateTime.AddSeconds(-1);
            }

            foreach (var post in cachedPosts)
            {
                yield return post;

                await Task.Delay(5000);
            }

            cachedPosts.InsertRange(0, newPosts);
            _cache.Set(cacheKey, cachedPosts.ToArray(), _cacheOptions);
        }
        finally
        {
            PostsLock.Release();
        }
    }

    async Task<FacebookTokenDetails> IFacebookService.GetTokenDetailsAsync(string token, CancellationToken ct)
    {
        var url = $"/debug_token?input_token={token}&access_token={_options.GroupAccessToken}";
        var tokenDetails = await _httpClient.GetFromJsonAsync<FacebookTokenDetails>(url, ct);
        return tokenDetails!;
    }
}
