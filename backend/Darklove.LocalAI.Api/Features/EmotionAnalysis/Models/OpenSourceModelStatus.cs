namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

public sealed record OpenSourceModelStatus(
    string Provider,
    string Model,
    bool RuntimeAvailable,
    bool ModelAvailable,
    string Status);
