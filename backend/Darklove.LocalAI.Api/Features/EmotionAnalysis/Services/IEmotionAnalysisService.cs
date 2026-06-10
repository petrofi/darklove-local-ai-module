using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface IEmotionAnalysisService
{
    Task<EmotionAnalysisResponse> AnalyzeAsync(
        string userText,
        CancellationToken cancellationToken = default);
}
