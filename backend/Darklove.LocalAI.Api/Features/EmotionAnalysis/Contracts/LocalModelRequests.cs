namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

public sealed record SelectLocalModelRequest(string? Model);

public sealed record DownloadLocalModelRequest(
    string? Model,
    string? Quantization = null);
