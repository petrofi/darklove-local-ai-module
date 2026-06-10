using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Endpoints;

public static class EmotionAnalysisEndpoints
{
    public const int MaximumTextLength = 2000;

    public static IEndpointRouteBuilder MapEmotionAnalysisEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/emotion")
            .WithTags("Emotion Analysis");

        group.MapPost("/analyze", Analyze)
            .WithName("AnalyzeEmotion")
            .WithSummary("Kullanıcı metnindeki duygusal işaretleri analiz eder.")
            .WithDescription(
                "Türkçe metni kural tabanlı olarak analiz eder. Sonuçlar tıbbi teşhis değildir. " +
                "Örnek istek: { \"userText\": \"Bugün kendimi yalnız ve yorgun hissediyorum.\" }")
            .Accepts<EmotionAnalysisRequest>("application/json")
            .Produces<EmotionAnalysisResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<Results<Ok<EmotionAnalysisResponse>, ValidationProblem>> Analyze(
        EmotionAnalysisRequest request,
        IEmotionAnalysisService emotionAnalysisService,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(
                validationErrors,
                title: "İstek doğrulanamadı.",
                detail: "Lütfen kullanıcı metnini kontrol edip tekrar deneyin.");
        }

        var response = await emotionAnalysisService.AnalyzeAsync(
            request.UserText!,
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static Dictionary<string, string[]> Validate(EmotionAnalysisRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.UserText))
        {
            errors["userText"] = ["Kullanıcı metni boş bırakılamaz."];
        }
        else if (request.UserText.Length > MaximumTextLength)
        {
            errors["userText"] =
            [
                $"Kullanıcı metni en fazla {MaximumTextLength} karakter olabilir."
            ];
        }

        return errors;
    }
}
