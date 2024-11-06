using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Api.Facebook;

public class FacebookAuthorisationHeaderHandler(IOptionsSnapshot<FacebookOptions> options) : DelegatingHandler
{
    private readonly FacebookOptions _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var hasAuthorizationHeader = request.Headers.Contains(HeaderNames.Authorization);
        var isOAuthUri = request.RequestUri?.AbsolutePath.Contains("access_token") ?? false;
        if (!(hasAuthorizationHeader || isOAuthUri))
        {
            var accessToken = _options.AccessToken;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"{accessToken}");
        }
 
        return await base.SendAsync(request, cancellationToken);
    }
}
