namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

public sealed record ChatRequest(
    string? UserText,
    IReadOnlyList<ChatMessage>? History = null);

public sealed record ChatMessage(
    string Role,
    string Content);
