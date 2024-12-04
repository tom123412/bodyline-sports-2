using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web;
using Web.ContactDetails;
using Web.Facebook;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<FacebookOptions>(builder.Configuration.GetSection(key: nameof(FacebookOptions)));
builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection(key: nameof(ContactOptions)));

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient<FacebookApiClient>(client =>
{
    client.BaseAddress = new(builder.Configuration.GetValue<string>("ApiUrl") ?? "https://localhost:1234/");
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Admin", policy => policy.AddRequirements(new AdminRequirement()));
});

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
});

builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();


class AdminRequirement : IAuthorizationRequirement {}

class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var user = context.User;
        //context.Succeed(requirement);
        context.Fail();

        return Task.CompletedTask;
    }
}