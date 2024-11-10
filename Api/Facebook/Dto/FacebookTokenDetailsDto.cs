namespace Api.Facebook.Dto;

public class FacebookTokenDetailsDto
{
    public bool IsValid {get;set;}
    public required DateTimeOffset ExpiresAt { get; set; }
}
