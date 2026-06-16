namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

public sealed record ChatResponse(
    string AssistantMessage,
    string RiskLevel,
    bool NeedsSupportWarning,
    string AnalysisMethod,
    string? Model = null,
    string? FallbackReason = null);
