using System.Security.Claims;
using Api.Facebook;
using Api.Facebook.Model;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services.AddMemoryCache();

builder.Services.Configure<FacebookOptions>(builder.Configuration.GetSection(key: nameof(FacebookOptions)));

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireAssertion(context =>
        {
            var email = context.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Email);
            return true;
        }));


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //options.DefaultChallengeScheme = FacebookDefaults.AuthenticationScheme;
    })
    // .AddFacebook(facebookOptions =>
    // {
    //     facebookOptions.AppId = builder.Configuration["FacebookOptions:AppId"] ?? facebookOptions.AppId;
    //     facebookOptions.AppSecret = builder.Configuration["FacebookOptions:AppSecret"] ?? facebookOptions.AppSecret;
    //     facebookOptions.AccessDeniedPath = "/";
    //     facebookOptions.Events = new()
    //     {
    //         OnRedirectToAuthorizationEndpoint = (context) =>
    //         {
    //             context.Response.Redirect($"{context.RedirectUri}&config_id={builder.Configuration["FacebookOptions:ConfigId"]}");
    //             return Task.CompletedTask;
    //         },
    //     };
    //     facebookOptions.SaveTokens = true;
    // })
    .AddCookie()
    // .AddIdentityCookies()
    ;

builder.Services.AddCors();

builder.Services
    .AddHttpClient("Facebook", (httpClient) =>
    {
        httpClient.BaseAddress = new Uri("https://graph.facebook.com/v21.0/");
        httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
    })
    .AddHttpMessageHandler<FacebookAuthorisationHeaderHandler>()
    ;

builder.Services.AddTransient<FacebookAuthorisationHeaderHandler>();
builder.Services.AddScoped<IFacebookService, FacebookService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(builder => 
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
);

app.MapGet("/facebook/groups/{id}", async Task<Results<Ok<FacebookGroup>, NotFound>> (string id, [FromServices]IFacebookService service) =>
{
    var group = await service.GetGroupAsync(id);
    return group is null ? TypedResults.NotFound(): TypedResults.Ok(group);
})
.WithName("GetFacebookGroupDetails")
.WithOpenApi();

app.MapGet("/facebook/groups/{id}/posts", async (string id, [FromServices]IFacebookService service) =>
{
    var posts = await service.GetPostsForGroupAsync(id);
    return TypedResults.Ok(posts);
})
.WithName("GetFacebookGroupPosts")
.WithOpenApi();

app.MapGet("/facebook/longlivedtoken", async ([FromQuery]string userAccessToken, [FromServices]IFacebookService service) =>
{
    var tokenDetails = await service.GetLongLivedTokenDetailsAsync(userAccessToken);
    return TypedResults.Ok(tokenDetails.ToDto());
})
.WithName("GetFacebookLongLivedTokenDetails")
.WithOpenApi();

app.Run();


