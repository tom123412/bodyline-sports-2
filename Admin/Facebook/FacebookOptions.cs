namespace Admin.Facebook;

public sealed class FacebookOptions
{
    public required string AccessToken { get; set; }
    public required string AppId { get; set; }
    public required string AppSecret { get; set; }
    public required string[] Administrators { get; set; }
    public string GraphUri { get; set; } = "https://graph.facebook.com/v23.0/";

}