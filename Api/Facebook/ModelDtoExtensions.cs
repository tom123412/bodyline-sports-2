using Api.Facebook.Dto;
using Api.Facebook.Model;

namespace Api.Facebook;

public static class ModelDtoExtensions
{
    public static FacebookTokenDetailsDto ToDto(this FacebookTokenDetails model)
    {
        return new FacebookTokenDetailsDto(model.Data.IsValid, model.Data.ExpiresAt);
    }

    public static FacebookGroupDto ToDto(this FacebookGroup model)
    {
        return new FacebookGroupDto(model.Id, model.Description, new FacebookPhotoDto(model.Cover.Source));
    }

    public static FacebookPostDto ToDto(this FacebookPost model)
    {
        return new FacebookPostDto(model.Id, model.Message, model.UpdatedDateTime, model.Type, 
            model.Attachments?.ToDto() ?? [], [.. model.Tags.Select(t => new FacebookTagDto(t.Name))]);
    }

    public static FacebookAttachmentDto[] ToDto(this FacebookAttachments model)
    {
        return [.. model.Data.Where(a => a.Media is not null).Select(a => new FacebookAttachmentDto(a.Media!.Image.ToDto(), a.SubAttachments?.ToDto() ?? [], a.Title))];
    }    

    public static FacebookSubAttachmentDto[] ToDto(this FacebookSubAttachments model)
    {
        return [.. model.Data.Select(a => new FacebookSubAttachmentDto(a.Media.Image.ToDto(), a.Description))];
    }    

    public static FacebookImageDto ToDto(this FacebookImage model)
    {
        return new FacebookImageDto(model.Height, model.Width, model.Src);
    }    
}
