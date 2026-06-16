using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/chat", Reply)
            .WithName("ReplyWithLocalChat")
            .WithTags("Local Chat")
            .WithSummary("Yerel açık kaynak model ile sade Türkçe sohbet yanıtı üretir.")
            .WithDescription(
                "Varsayılan CMD istemcisinin kullandığı sade sohbet endpointidir. " +
                "Duygu analizi raporu üretmez; normal araştırma ve sohbet sorularını doğal biçimde yanıtlar.")
            .Accepts<ChatRequest>("application/json")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<Results<Ok<ChatResponse>, ValidationProblem>> Reply(
        ChatRequest request,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(
                validationErrors,
                title: "İstek doğrulanamadı.",
                detail: "Lütfen sohbet metnini kontrol edip tekrar deneyin.");
        }

        return TypedResults.Ok(await chatService.ReplyAsync(request, cancellationToken));
    }

    private static Dictionary<string, string[]> Validate(ChatRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.UserText))
        {
            errors["userText"] = ["Sohbet metni boş bırakılamaz."];
        }
        else if (request.UserText.Length > EmotionAnalysisEndpoints.MaximumTextLength)
        {
            errors["userText"] =
            [
                $"Sohbet metni en fazla {EmotionAnalysisEndpoints.MaximumTextLength} karakter olabilir."
            ];
        }

        return errors;
    }
}
