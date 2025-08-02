using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using System.Security.Claims;
using Admin.Components;
using FacebookOptions = Admin.Facebook.FacebookOptions;
using Microsoft.AspNetCore.HttpOverrides;
using Azure.Identity;
using Microsoft.Net.Http.Headers;
using Admin.Azure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureAppConfiguration(options =>
{
    var connectionString = builder.Configuration["AzureOptions:AppConfigurationConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        options.Connect(new Uri(builder.Configuration["AzureOptions:AppConfigurationEndpoint"]!), new DefaultAzureCredential());
    }
    else
    {
        options.Connect(connectionString);
    }

    options.ConfigureRefresh(configure =>
    {
        const string AccessTokenKey = $"{nameof(FacebookOptions)}:{nameof(FacebookOptions.AccessToken)}";
        configure
            .Register($"{AccessTokenKey}", refreshAll: true)
            .SetRefreshInterval(TimeSpan.FromSeconds(1))
            ;
    });
});

builder.Services.Configure<FacebookOptions>(builder.Configuration.GetSection(key: nameof(FacebookOptions)));
builder.Services.Configure<AzureOptions>(builder.Configuration.GetSection(key: nameof(AzureOptions)));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireAssertion(context =>
        {
            var adminEmails = builder.Configuration.Get<AppSettings>()?.FacebookOptions.Administrators ?? [];
            var email = context.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Email);
            return adminEmails.Contains(email?.Value);
        }));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = FacebookDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["FacebookOptions:AppId"] ?? options.AppId;
        options.AppSecret = builder.Configuration["FacebookOptions:AppSecret"] ?? options.AppSecret;
        options.SaveTokens = true;
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor 
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
});

builder.Services
    .AddHttpClient("Facebook", (sp, httpClient) =>
    {
        var facebookOptions = sp.GetRequiredService<IOptions<FacebookOptions>>().Value;
        httpClient.BaseAddress = new Uri(facebookOptions.GraphUri);
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
    })
    ;

builder.Services.AddHsts(options =>
{
    options.Preload = true; // Allow inclusion in HSTS preload list
    options.IncludeSubDomains = true; // Apply HSTS to all subdomains
    options.MaxAge = TimeSpan.FromDays(365); // 1 year max age
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


internal record AppSettings(FacebookOptions FacebookOptions);
