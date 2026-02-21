using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Facebook;
public record TokenDetails(bool IsValid, DateTimeOffset ExpiresAt);
