namespace Api.Facebook.Dto;

public record FacebookPostDto(string Id, string? Message, DateTimeOffset UpdatedDateTime, 
    string? Type, FacebookAttachmentDto[] Attachments, FacebookTagDto[] Tags);
