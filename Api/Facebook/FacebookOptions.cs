namespace Api.Facebook;

public sealed class FacebookOptions
{
    public required string AccessToken { get; set; }
    public required string AppId { get; set; }
    public required string AppSecret { get; set; }
    public required int PostsToLoad { get; set; }
    public string[] TagsToHide { get; set; } = [];
}
