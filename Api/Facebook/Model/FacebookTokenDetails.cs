using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Facebook.Model;

public class FacebookTokenDetails
{
    public required TokenData Data {get;set;}
}

public class TokenData
{
    [JsonPropertyName("is_valid")]
    public bool IsValid {get;set;}

    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(UnixTimeConverter))]
    public required DateTimeOffset ExpiresAt { get; set; }
}

class UnixTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        var unixTime = reader.GetDouble();
        return epoch.AddSeconds(unixTime);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}