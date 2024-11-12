namespace Api.Facebook.Dto;

public record FacebookTokenDetailsDto(bool IsValid, DateTimeOffset ExpiresAt);
