using System.Text.Json.Serialization;

namespace Api.Facebook.Model;

public class FacebookRefreshedTokenDetails
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
}
