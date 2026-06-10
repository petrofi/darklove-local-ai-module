namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

public sealed record OpenSourceModelClassification(
    string DetectedEmotion,
    double Confidence,
    IReadOnlyDictionary<string, double> Scores);
