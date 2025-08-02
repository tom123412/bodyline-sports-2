using System.Net;
using Api.Facebook;
using Asp.Versioning;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;

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

builder.Services.AddTransient<FacebookAuthorisationHeaderHandler>();
builder.Services.AddScoped<IFacebookService, FacebookService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
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

app.Run();