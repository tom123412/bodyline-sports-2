using Api.Facebook.Dto;
using Api.Facebook.Model;

namespace Api.Facebook;

public static class ModelDtoExtensions
{
    public static FacebookTokenDetailsDto ToDto(this FacebookTokenDetails model)
    {
        return new FacebookTokenDetailsDto
        {
            IsValid = model.Data.IsValid,
            ExpiresAt = model.Data.ExpiresAt,
        };
    }
}
