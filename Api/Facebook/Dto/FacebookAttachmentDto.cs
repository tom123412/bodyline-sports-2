namespace Api.Facebook.Dto;

public record FacebookAttachmentDto(FacebookImageDto? Image, FacebookSubAttachmentDto[] SubAttachments, string Title = "post image");

public record FacebookImageDto(int Height, int Width, Uri Src);

public record FacebookSubAttachmentDto(FacebookImageDto Image, string Title = "post image");
