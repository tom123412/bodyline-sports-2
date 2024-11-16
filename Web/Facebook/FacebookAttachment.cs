namespace Web.Facebook;

public record FacebookAttachment(FacebookImage Image, FacebookSubAttachment[] SubAttachments, string Title);

public record FacebookImage(int Height, int Width, Uri Src);

public record FacebookSubAttachment(FacebookImage Image, string Title);
