namespace Api.Facebook;

public class FacebookAttachments
{
    public required AttachmentsData[] Data { get; set; }
}

public class AttachmentsData
{
    public required Media Media { get; set; }
    public SubAttachments? SubAttachments { get; set; }
    public string Title { get; set; } = "post image";
}

public class Media
{
    public required Image Image { get; set; }
}

public class Image
{
    public required int Height { get; set;}
    public required int Width { get; set;}
    public required Uri Src { get; set;}
}

public class SubAttachments
{
    public required SubAttachmentsData[] Data { get; set; }
}

public class SubAttachmentsData
{
    public string Description { get; set; } = "Post image";
    public required Media Media { get; set; }
}