using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Web.Facebook;

public partial record FacebookPost(string Id, string? Message, DateTimeOffset UpdatedDateTime,
    string? Type, FacebookAttachment[] Attachments, FacebookTag[] Tags)
{
    public string[] Reels => [.. MyRegex().Matches(Message ?? string.Empty).Select(m => m.Groups[1].Value)];

    [GeneratedRegex(@"(https://www.facebook.com/reel/[0-9]+)")]
    private static partial Regex MyRegex();
}

public record FacebookTag(string Name);
