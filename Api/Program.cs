using System.Net;
using System.Security.Claims;
using Api.Azure;
using Api.Facebook;
using Api.Facebook.Model;
using Asp.Versioning;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using FacebookOptions = Api.Facebook.FacebookOptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            ;
    })
    .UseAzureMonitor();

builder.Services.AddHealthChecks();

builder.Services.AddMemoryCache();

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options
        .Connect(new Uri(builder.Configuration["AzureOptions:AppConfigurationEndpoint"]!), new DefaultAzureCredential())
        .ConfigureRefresh(configure =>
        {
            const string AccessTokenKey = $"{nameof(FacebookOptions)}:{nameof(FacebookOptions.AccessToken)}";
            configure
                .Register($"{AccessTokenKey}", refreshAll: true)
                .SetRefreshInterval(TimeSpan.FromSeconds(1))
                ;
        })
        ;
});

builder.Services.Configure<FacebookOptions>(builder.Configuration.GetSection(key: nameof(FacebookOptions)));

builder.Services.AddCors();

builder.Services
    .AddHttpClient("Facebook", (sp, httpClient) =>
    {
        var facebookOptions = sp.GetRequiredService<IOptions<FacebookOptions>>().Value;
        httpClient.BaseAddress = new Uri(facebookOptions.GraphUri);
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
    })
    .AddHttpMessageHandler<FacebookAuthorisationHeaderHandler>()
    ;

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<FacebookAuthorisationHeaderHandler>();
builder.Services.AddScoped<IFacebookService, FacebookService>();
builder.Services.AddScoped<IAzureService, AzureService>();
builder.Services.AddScoped(sp =>
{
    var azureOptions = sp.GetRequiredService<IOptions<AzureOptions>>().Value;
    if (string.IsNullOrWhiteSpace(azureOptions.AppConfigurationConnectionString))
    {
        return new ConfigurationClient(azureOptions.AppConfigurationEndpoint, new DefaultAzureCredential());
    }
    else
    {
        return new ConfigurationClient(azureOptions.AppConfigurationConnectionString);
    }
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    })
    ;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.Headers.Append("Reason", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var facebookOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<FacebookOptions>>().Value;

                var httpClient = context.HttpContext.RequestServices
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient();

                var accessToken = context.SecurityToken?.ToString();
                var response = await httpClient.GetAsync(
                    $"{facebookOptions.GraphUri}/me?fields=id,name,email&access_token={accessToken}");

                if (!response.IsSuccessStatusCode)
                {
                    context.Fail("Invalid Facebook token");
                    return;
                }

                var userData = await response.Content.ReadFromJsonAsync<FacebookUserDetails>();
                if (userData == null)
                {
                    context.Fail("Invalid Facebook user data");
                    return;
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userData.Id),
                    new(ClaimTypes.Name, userData.Name),
                    new(ClaimTypes.Email, userData.Email)
                };

                // set up the user's identity with Facebook data
                context.Principal = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme));
            }
        };

        // disable JWT validation since we're using Facebook tokens
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false
        };
    });

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

app.UseAuthentication();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseCors(builder =>
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
);

app.UseExceptionHandler(applicationBuilder =>
{
    applicationBuilder.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error is HttpRequestException hre && hre.StatusCode == HttpStatusCode.Unauthorized)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        }
        else
        {
            await Results.Problem().ExecuteAsync(context);
        }
    });
});

app.MapHealthChecks("/health");
app.MapFacebookEndpoints();
app.MapAzureEndpoints();

app.Run();