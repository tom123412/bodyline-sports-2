using Api.Facebook;
using Api.Facebook.Dto;
using Api.Facebook.Model;
using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry().UseAzureMonitor();

builder.Services.AddHealthChecks();

builder.Services.AddMemoryCache();

builder.Services.Configure<FacebookOptions>(builder.Configuration.GetSection(key: nameof(FacebookOptions)));

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

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
//.AddMvc() // This is needed for controllers
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

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

app.MapHealthChecks("/health");

var apiVersionSet = app
    .NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var apiGroup = app
    .MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

apiGroup.MapGet("/facebook/groups/{id}", async Task<Results<Ok<FacebookGroupDto>, NotFound>> (string id, [FromServices]IFacebookService service) =>
{
    var group = await service.GetGroupAsync(id);
    return group is null ? TypedResults.NotFound(): TypedResults.Ok(group.ToDto());
})
.WithOpenApi();

apiGroup.MapGet("/facebook/groups/{id}/posts", async (string id, [FromServices]IFacebookService service) =>
{
    var posts = await service.GetPostsForGroupAsync(id);
    return TypedResults.Ok(posts.Select(p => p.ToDto()));
})
.WithOpenApi();

apiGroup.MapGet("/facebook/longlivedtoken", async ([FromQuery]string userAccessToken, [FromServices]IFacebookService service) =>
{
    var tokenDetails = await service.GetLongLivedTokenDetailsAsync(userAccessToken);
    return TypedResults.Ok(tokenDetails.ToDto());
})
.WithOpenApi();

app.Run();