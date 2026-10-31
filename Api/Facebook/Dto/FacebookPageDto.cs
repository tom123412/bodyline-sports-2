namespace Api.Facebook.Dto;

public record FacebookPageDto(string Id, string About, FacebookPhotoDto Cover, string[] Emails, string Phone, string Address);
