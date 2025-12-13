using System;
using Api.Facebook.Dto;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Facebook;

public static class FacebookEndpoints
{
    extension(WebApplication app)
    {
        public void MapFacebookEndpoints()
        {
            var apiVersionSet = app
                .NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1))
                .ReportApiVersions()
                .Build();

            var apiGroup = app
                .MapGroup("api/facebook")
                .WithApiVersionSet(apiVersionSet)
                ;

            apiGroup.MapGet("/groups/{id}", async Task<Results<Ok<FacebookGroupDto>, NotFound>> (string id, [FromServices] IFacebookService service, CancellationToken ct) =>
            {
                var group = await service.GetGroupAsync(id, ct);
                return group is null ? TypedResults.NotFound() : TypedResults.Ok(group.ToDto());
            });

            apiGroup.MapGet("/groups/{id}/posts", async (string id, [FromServices] IFacebookService service, CancellationToken ct) =>
            {
                var posts = await service.GetPostsForGroupAsync(id, ct);
                return TypedResults.Ok(posts.Select(p => p.ToDto()));
            });

            apiGroup.MapGet("/groups/{groupId}/posts/{postId}", async Task<Results<Ok<FacebookPostDto>, NotFound>> (string groupId, string postId, [FromServices] IFacebookService service, CancellationToken ct) =>
            {
                var posts = await service.GetPostsForGroupAsync(groupId, ct);
                var post = posts.SingleOrDefault(p => p.Id == postId);
                return post is null ? TypedResults.NotFound() : TypedResults.Ok(post.ToDto());
            });

            apiGroup.MapGet("/longlivedtoken", async ([FromQuery] string userAccessToken, [FromServices] IFacebookService service, CancellationToken ct) =>
            {
                var tokenDetails = await service.GetLongLivedTokenDetailsAsync(userAccessToken, ct);
                return TypedResults.Ok(tokenDetails.ToDto());
            });

            apiGroup.MapGet("/pages/{id}/posts", async (string id, [FromServices] IFacebookService service, CancellationToken ct) =>
            {
                var posts = await service.GetPostsForPageAsync(id, ct);
                return TypedResults.Ok(posts.Select(p => p.ToDto()));
            });
        }

        public void MapFacebookEndpoints2()
        {
            var apiVersionSet = app
                .NewApiVersionSet()
                .HasApiVersion(new ApiVersion(2))
                .ReportApiVersions()
                .Build();

            var apiGroup = app
                .MapGroup("api/facebook")
                .WithApiVersionSet(apiVersionSet)
                ;

            apiGroup.MapGet("/groups/{id}/posts", async (string id, [FromServices] IFacebookService service, CancellationToken ct) =>
            {
                var posts = service.GetPostsForGroupAsync2(id, ct);
                return TypedResults.ServerSentEvents(posts.Select(p => p.ToDto()), "posts");
            });
        }
    }
}
