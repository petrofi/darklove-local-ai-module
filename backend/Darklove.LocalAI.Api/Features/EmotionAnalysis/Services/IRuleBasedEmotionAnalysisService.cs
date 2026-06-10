using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface IRuleBasedEmotionAnalysisService
{
    EmotionAnalysisResponse Analyze(string userText);
}
