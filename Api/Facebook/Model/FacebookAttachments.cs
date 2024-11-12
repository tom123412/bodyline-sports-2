namespace Api.Facebook.Model;

public record FacebookAttachments(AttachmentsData[] Data);

public record AttachmentsData(Media Media, FacebookSubAttachments? SubAttachments, string Title = "post image");

public record Media(FacebookImage Image);

public record FacebookImage(int Height, int Width, Uri Src);

public record FacebookSubAttachments(SubAttachmentsData[] Data);

public record SubAttachmentsData(Media Media, string Description = "post image");