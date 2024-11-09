using Api.Facebook;
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/facebook/groups/{id}", async Task<Results<Ok<FacebookGroup>, NotFound>> (string id, [FromServices]IFacebookService service) =>
{
    var group = await service.GetGroupAsync(id);
    return group is null ? TypedResults.NotFound(): TypedResults.Ok(group);
})
.WithName("GetFacebookGroupDetails")
.WithOpenApi();


app.MapGet("/facebook/groups/{id}/posts", async (string id, [FromQuery]int? limit, [FromServices]IFacebookService service) =>
{
    var posts = await service.GetPostsForGroupAsync(id, limit);
    return TypedResults.Ok(posts);
})
.WithName("GetFacebookGroupPosts")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


