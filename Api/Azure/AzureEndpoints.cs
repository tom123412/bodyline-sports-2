using Api.Facebook;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Azure;

public static class AzureEndpoints
{
    public static void MapAzureEndpoints(this WebApplication app)
    {
        var apiVersionSet = app
            .NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var apiGroup = app
            .MapGroup("api/azure")
            .WithApiVersionSet(apiVersionSet)
            ;

        apiGroup
            .MapPut("/accesstoken", Task<NoContent> ([FromServices] IHttpContextAccessor httpContextAccessor, [FromServices] IAzureService service) =>
            {
                var authFeatures = httpContextAccessor.HttpContext?.Features.Get<IAuthenticateResultFeature>();
                var userAccessToken = authFeatures?.AuthenticateResult!.Properties!.GetTokenValue("access_token");

                service.SaveConfigurationSettingsAsync(
                    $"{nameof(FacebookOptions)}:{nameof(FacebookOptions.AccessToken)}",
                    userAccessToken!
                );
                return Task.FromResult(TypedResults.NoContent());
            })
            .RequireAuthorization()
            ;
    }
}
