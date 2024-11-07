namespace Web.Facebook;

public sealed class FacebookOptions
{
    public required string GroupId { get; set; }
    public required string DefaultAboutMessage { get; set;}
    public required Uri DefaultLogoUrl { get; set;}
}