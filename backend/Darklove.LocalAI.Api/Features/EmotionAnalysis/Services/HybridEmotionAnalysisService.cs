using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed class HybridEmotionAnalysisService(
    IRuleBasedEmotionAnalysisService ruleBasedService,
    IOpenSourceModelClient modelClient,
    ILocalModelSelection selection,
    IOptions<LocalModelOptions> options,
    ILogger<HybridEmotionAnalysisService> logger) : IEmotionAnalysisService
{
    private readonly LocalModelOptions _options = options.Value;

    public async Task<EmotionAnalysisResponse> AnalyzeAsync(
        string userText,
        CancellationToken cancellationToken = default)
    {
        var ruleBasedResult = ruleBasedService.Analyze(userText);

        // Safety-critical phrases always use deterministic handling.
        if (ruleBasedResult.NeedsSupportWarning)
        {
            return ruleBasedResult;
        }

        if (!_options.Enabled)
        {
            return ruleBasedResult;
        }

        try
        {
            var modelResult = await modelClient.ClassifyAsync(userText, cancellationToken);

            return ruleBasedResult with
            {
                DetectedEmotion = modelResult.DetectedEmotion,
                Confidence = modelResult.Confidence,
                MotivationMessage =
                    EmotionResponsePolicy.GetMotivationMessage(modelResult.DetectedEmotion),
                AnalysisMethod = "open-source-model",
                Model = selection.Model,
                ModelScores = modelResult.Scores,
                FallbackReason = null
            };
        }
        catch (LocalModelException exception)
        {
            logger.LogWarning(
                exception,
                "Yerel model kullanılamadı. Kural tabanlı analize geçiliyor. ReasonCode: {ReasonCode}",
                exception.ReasonCode);

            return ruleBasedResult with
            {
                AnalysisMethod = "rule-based-fallback",
                Model = selection.Model,
                FallbackReason = exception.ReasonCode
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Yerel model zaman aşımına uğradı. Kural tabanlı analize geçiliyor.");

            return ruleBasedResult with
            {
                AnalysisMethod = "rule-based-fallback",
                Model = selection.Model,
                FallbackReason = "model-timeout"
            };
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Yerel model çalışma zamanına ulaşılamadı. Kural tabanlı analize geçiliyor.");

            return ruleBasedResult with
            {
                AnalysisMethod = "rule-based-fallback",
                Model = selection.Model,
                FallbackReason = "model-unavailable"
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Yerel model analizinde beklenmeyen hata oluştu. Kural tabanlı analize geçiliyor.");

            return ruleBasedResult with
            {
                AnalysisMethod = "rule-based-fallback",
                Model = selection.Model,
                FallbackReason = "model-error"
            };
        }
    }
}
