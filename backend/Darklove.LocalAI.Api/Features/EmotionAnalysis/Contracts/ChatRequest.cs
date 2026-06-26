namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

public sealed record ChatRequest(
    string? UserText,
    IReadOnlyList<ChatMessage>? History = null,
    HeartContext? HeartContext = null);

public sealed record ChatMessage(
    string Role,
    string Content);

public sealed record HeartContext(
    int? Bpm,
    string? Rhythm,
    string? SignalQuality,
    bool? LeadOff,
    int? SampleCount,
    double? AverageValue,
    DateTimeOffset? MeasuredAt);
