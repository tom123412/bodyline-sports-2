using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Web.Facebook;

public class FacebookApiClient(HttpClient httpClient, IOptions<FacebookOptions> options, IMemoryCache cache)
{
    private readonly FacebookOptions _options = options.Value;

    public async Task<FacebookGroup?> GetGroup(CancellationToken cancellationToken = default)
    {
        var x = httpClient;
        var group = await httpClient.GetFromJsonAsync<FacebookGroup>($"/api/facebook/groups/{_options.GroupId}", cancellationToken: cancellationToken);
        return group;
    }

    public async Task<IEnumerable<FacebookPost>> GetPostsForGroup(CancellationToken cancellationToken = default)
    {
        const string PostsCacheKey = "FacebookPosts";
        var CacheDuration = TimeSpan.FromMinutes(5);

        return (await cache.GetOrCreateAsync(PostsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var posts = await httpClient.GetFromJsonAsync<IEnumerable<FacebookPost>>(
                $"/api/facebook/groups/{_options.GroupId}/posts",
                cancellationToken: cancellationToken);

            return posts ?? [];
        })) ?? [];
    }
}
