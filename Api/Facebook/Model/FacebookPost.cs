using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Facebook.Model;

public class FacebookPost
{
    public string? Message { get; set; }
    public required string Id { get; set; } 
    public FacebookAttachments? Attachments { get; set; } 
    
    [JsonPropertyName("updated_time")]
    [JsonConverter(typeof(FacebookDateTimeConverter))]
    public required DateTimeOffset UpdatedDateTime { get; set; }
    
    public string? Type { get; set; }

    [JsonPropertyName("message_tags")]
    public FacebookTag[] Tags { get; set; } = [];
}

class FacebookDateTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.Parse(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var json = JsonSerializer.Serialize(value, options);
        writer.WriteStringValue(json.Replace("\"", ""));
    }
}