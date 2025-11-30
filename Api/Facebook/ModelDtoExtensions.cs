using Api.Facebook.Dto;
using Api.Facebook.Model;

namespace Api.Facebook;

public static class ModelDtoExtensions
{
    extension(FacebookTokenDetails model)
    {
        public FacebookTokenDetailsDto ToDto()        
        {
            return new FacebookTokenDetailsDto(model.Data.IsValid, model.Data.ExpiresAt);
        }
    }

    extension(FacebookGroup model)
    {
        public FacebookGroupDto ToDto()
        {
            return new FacebookGroupDto(model.Id, model.Description, new FacebookPhotoDto(model.Cover.Source));
        }
    }

    extension(FacebookPost model)
    {
        public FacebookPostDto ToDto()
        {
            return new FacebookPostDto(model.Id, model.Message, model.UpdatedDateTime, model.Type,
                model.Attachments?.ToDto() ?? [], [.. model.Tags.Select(t => new FacebookTagDto(t.Name))]);
        }
    }

    extension(FacebookAttachments model)
    {
        public FacebookAttachmentDto[] ToDto()
        {
            return [.. model.Data.Where(a => a.Media is not null).Select(a => new FacebookAttachmentDto(a.Media!.Image.ToDto(), a.SubAttachments?.ToDto() ?? [], a.Title))];
        }
    }

    extension(FacebookSubAttachments model)
    {
        public FacebookSubAttachmentDto[] ToDto()
        {
            return [.. model.Data.Select(a => new FacebookSubAttachmentDto(a.Media.Image.ToDto(), a.Description))];
        }
    }

    extension(FacebookImage model)
    {
        public FacebookImageDto ToDto()
        {
            return new FacebookImageDto(model.Height, model.Width, model.Src);
        }
    }
}
