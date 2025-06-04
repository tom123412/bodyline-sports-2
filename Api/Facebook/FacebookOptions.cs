namespace Api.Facebook;

public sealed class FacebookOptions
{
    public required string AccessToken { get; set; }
    public required string AppId { get; set; }
    public required string AppSecret { get; set; }
    public required int PostsToLoad { get; set; }
    public string[] TagsToHide { get; set; } = [];
    public string GraphUri { get; set; } = "https://graph.facebook.com/v23.0/";
    public int HoursToCache { get; set; } = 2;
}
