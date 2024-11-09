using System.Text.Json.Serialization;

namespace Web.Facebook;

public class FacebookPost
{
    public string? Message { get; set; }
    public required string Id { get; set; } 
    public FacebookAttachments? Attachments { get; set; } 
    
    [JsonPropertyName("updated_time")]
    public required DateTimeOffset UpdatedDateTime { get; set; }
    
    public string? Type { get; set; }
}
