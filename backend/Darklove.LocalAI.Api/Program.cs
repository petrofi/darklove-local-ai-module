using Darklove.LocalAI.Api.Features.EmotionAnalysis.Endpoints;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Darklove.LocalAI.Api.Infrastructure.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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
builder.Services.AddSingleton<IEmotionAnalysisService, RuleBasedEmotionAnalysisService>();

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

app.MapHealthEndpoint();
app.MapEmotionAnalysisEndpoints();

app.Run();

public partial class Program;
