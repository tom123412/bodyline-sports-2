using System;
using Api.Facebook.Dto;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Facebook;

public static class FacebookEndpoints
{
    public static void MapFacebookEndpoints(this WebApplication app)
    {
        var apiVersionSet = app
            .NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var apiGroup = app
            .MapGroup("api/facebook")
            .WithApiVersionSet(apiVersionSet)
            .WithOpenApi();

        apiGroup.MapGet("/groups/{id}", async Task<Results<Ok<FacebookGroupDto>, NotFound>> (string id, [FromServices] IFacebookService service) =>
        {
            var group = await service.GetGroupAsync(id);
            return group is null ? TypedResults.NotFound() : TypedResults.Ok(group.ToDto());
        });

        apiGroup.MapGet("/groups/{id}/posts", async (string id, [FromServices] IFacebookService service) =>
        {
            var posts = await service.GetPostsForGroupAsync(id);
            return TypedResults.Ok(posts.Select(p => p.ToDto()));
        });

        apiGroup.MapGet("/groups/{groupId}/posts/{postId}", async Task<Results<Ok<FacebookPostDto>, NotFound>> (string groupId, string postId, [FromServices] IFacebookService service) =>
        {
            var posts = await service.GetPostsForGroupAsync(groupId);
            var post = posts.SingleOrDefault(p => p.Id == postId);
            return post is null ? TypedResults.NotFound() : TypedResults.Ok(post.ToDto());
        });

        apiGroup.MapGet("/longlivedtoken", async ([FromQuery] string userAccessToken, [FromServices] IFacebookService service) =>
        {
            var tokenDetails = await service.GetLongLivedTokenDetailsAsync(userAccessToken);
            return TypedResults.Ok(tokenDetails.ToDto());
        });
    }
}
