using System.Text.Json.Serialization;

namespace Api.Facebook.Model;

public record FacebookPage(string About, FacebookPhoto Cover, string[] Emails, string Phone, [property: JsonPropertyName("single_line_address")] string Address, string Id);
