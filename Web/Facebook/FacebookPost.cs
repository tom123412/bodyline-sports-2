using System.Text.Json.Serialization;

namespace Web.Facebook;

public record FacebookPost(string Id, string? Message, DateTimeOffset UpdatedDateTime, 
    string? Type, FacebookAttachment[] Attachments);
