using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Admin.Facebook;

public class Facebook(IHttpClientFactory httpClientFactory, IOptions<FacebookOptions> options)
{
    private class AccessTokenDetails
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }
    }

    private class Field
    {
        public required string Id { get; set; }

        [JsonPropertyName("object_id")]
        public string? ObjectId { get; set; }
    }

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Facebook");
    private readonly FacebookOptions _options = options.Value;

    public async Task<TokenDetails> GetTokenDetails(string accessToken, string userAccessToken)
    {
        var url = $"/debug_token?input_token={accessToken}&access_token={userAccessToken}";
        var tokenDetails = await _httpClient.GetFromJsonAsync<TokenDetails>(url);
        return tokenDetails!;
    }

    public async Task<string> ExchangeForLongLivedToken(string accessToken)
    {
        var url = $"/oauth/access_token?grant_type=fb_exchange_token&client_id={_options.AppId}&client_secret={_options.AppSecret}&fb_exchange_token={accessToken}";
        var longLivedToken = await _httpClient.GetFromJsonAsync<AccessTokenDetails>(url);
        return longLivedToken!.AccessToken;
    }
}