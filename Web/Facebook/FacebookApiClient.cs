using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

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

        return posts ?? Array.Empty<FacebookPost>();
    }

    public async IAsyncEnumerable<FacebookPost?> GetPostsForGroupSSE([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/facebook/groups/{_options.GroupId}/posts?api-version=2.0");
        request.SetBrowserResponseStreamingEnabled(true);

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var parser = SseParser.Create(stream, (type, data) =>
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<FacebookPost>(json);
        });

        await foreach (var post in parser.EnumerateAsync(cancellationToken))
        {
            yield return post.Data;
        }
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
