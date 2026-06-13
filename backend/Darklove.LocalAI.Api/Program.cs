using System.Text.Json.Serialization;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Endpoints;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Darklove.LocalAI.Api.Infrastructure.Health;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddExceptionHandler(options =>
{
    options.StatusCodeSelector = exception =>
        exception is BadHttpRequestException badRequestException
            ? badRequestException.StatusCode
            : StatusCodes.Status500InternalServerError;
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.Exception is BadHttpRequestException)
        {
            context.ProblemDetails.Title = "Geçersiz HTTP isteği.";
            context.ProblemDetails.Detail =
                "İstek gövdesi geçerli JSON biçiminde olmalıdır.";
        }

        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            context.HttpContext.TraceIdentifier);
    };
});
builder.Services.AddHealthChecks();
builder.Services
    .AddOptions<LocalModelOptions>()
    .Bind(builder.Configuration.GetSection(LocalModelOptions.SectionName))
    .Validate(
        options => !options.Enabled ||
            (Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) &&
             endpoint.Scheme is "http" or "https" &&
             endpoint.IsLoopback),
        "LocalModel:Endpoint güvenlik nedeniyle localhost, 127.0.0.1 veya ::1 olmalıdır.")
    .Validate(
        options => !options.Enabled ||
            string.Equals(options.Provider, "ollama", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.Provider, "lmstudio", StringComparison.OrdinalIgnoreCase),
        "Desteklenen LocalModel:Provider değerleri 'ollama' ve 'lmstudio'dur.")
    .Validate(
        options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Model),
        "LocalModel:Model boş bırakılamaz.")
    .Validate(
        options => options.TimeoutSeconds is >= 5 and <= 300,
        "LocalModel:TimeoutSeconds 5 ile 300 arasında olmalıdır.")
    .ValidateOnStart();

builder.Services.AddSingleton<IRuleBasedEmotionAnalysisService, RuleBasedEmotionAnalysisService>();
builder.Services.AddSingleton<ILocalModelSelection, LocalModelSelection>();

var configuredProvider = builder.Configuration[
    $"{LocalModelOptions.SectionName}:Provider"] ?? "ollama";

if (string.Equals(configuredProvider, "lmstudio", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ILocalModelRuntimeLauncher, LmStudioRuntimeLauncher>();
    builder.Services.AddHttpClient<LmStudioOpenSourceModelClient>(
        ConfigureLocalModelHttpClient);
    builder.Services.AddScoped<IOpenSourceModelClient>(serviceProvider =>
        serviceProvider.GetRequiredService<LmStudioOpenSourceModelClient>());
    builder.Services.AddScoped<ILocalModelManager>(serviceProvider =>
        serviceProvider.GetRequiredService<LmStudioOpenSourceModelClient>());
}
else
{
    builder.Services.AddHttpClient<OllamaOpenSourceModelClient>(
        ConfigureLocalModelHttpClient);
    builder.Services.AddHttpClient<OllamaLocalModelManager>(
        ConfigureLocalModelHttpClient);
    builder.Services.AddScoped<IOpenSourceModelClient>(serviceProvider =>
        serviceProvider.GetRequiredService<OllamaOpenSourceModelClient>());
    builder.Services.AddScoped<ILocalModelManager>(serviceProvider =>
        serviceProvider.GetRequiredService<OllamaLocalModelManager>());
}

builder.Services.AddScoped<IEmotionAnalysisService, HybridEmotionAnalysisService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Darklove Local AI API v1");
        options.DocumentTitle = "Darklove Local AI API";
    });
}
else
{
    app.UseHsts();
}

if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.MapHealthEndpoint();
app.MapOpenSourceModelEndpoints();
app.MapEmotionAnalysisEndpoints();

app.Run();

static void ConfigureLocalModelHttpClient(
    IServiceProvider serviceProvider,
    HttpClient client)
{
    var options = serviceProvider
        .GetRequiredService<IOptions<LocalModelOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.Endpoint);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}

public partial class Program;
