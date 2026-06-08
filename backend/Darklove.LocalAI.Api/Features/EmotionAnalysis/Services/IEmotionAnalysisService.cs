using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface IEmotionAnalysisService
{
    EmotionAnalysisResponse Analyze(string userText);
}
