using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Web.Facebook;

public class FacebookApiClient(HttpClient httpClient, IOptions<FacebookOptions> options)
{
    private readonly FacebookOptions _options = options.Value;

    public async Task<FacebookGroup?> GetGroup(CancellationToken cancellationToken = default)
    {
        var group = await httpClient.GetFromJsonAsync<FacebookGroup>($"/api/facebook/groups/{_options.GroupId}", cancellationToken);
        return group;
    }

    public async Task<IEnumerable<FacebookPost>> GetPostsForGroup(CancellationToken cancellationToken = default)
    {
        var posts = await httpClient.GetFromJsonAsync<IEnumerable<FacebookPost>>(
            $"/api/facebook/groups/{_options.GroupId}/posts", cancellationToken);

        return posts ?? [];
    }

    public async Task<IAsyncEnumerable<FacebookPost>> GetPostsForGroup2(CancellationToken cancellationToken = default)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/facebook/groups/{_options.GroupId}/posts");
        //httpRequestMessage.SetBrowserResponseStreamingEnabled(true);
        //httpRequestMessage.Headers.Add("Accept", "text/event-stream");

        var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
        var posts = await response.Content.ReadFromJsonAsync<IAsyncEnumerable<FacebookPost>>(cancellationToken);

        return posts ?? AsyncEnumerable.Empty<FacebookPost>();
    }
}
