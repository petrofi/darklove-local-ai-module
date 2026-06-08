namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

public sealed record EmotionAnalysisResponse(
    string DetectedEmotion,
    double Confidence,
    IReadOnlyDictionary<string, int> Scores,
    IReadOnlyDictionary<string, IReadOnlyList<string>> MatchedKeywords,
    string RiskLevel,
    bool NeedsSupportWarning,
    string MotivationMessage);
