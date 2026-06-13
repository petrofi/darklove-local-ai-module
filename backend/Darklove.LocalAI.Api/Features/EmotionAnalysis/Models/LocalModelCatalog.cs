namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

public sealed record LocalModelCatalog(
    string Provider,
    string SelectedModel,
    bool RuntimeAvailable,
    string Status,
    IReadOnlyList<LocalModelInfo> Models);

public sealed record LocalModelInfo(
    string Key,
    string DisplayName,
    string Provider,
    long SizeBytes,
    string? Parameters,
    string? Quantization,
    bool IsLoaded,
    bool SupportsVision);

public sealed record LocalModelSelectionResult(
    string Provider,
    string Model,
    bool Loaded,
    string Status);

public sealed record LocalModelDownloadStatus(
    string? JobId,
    string Status,
    long? TotalSizeBytes = null,
    long? DownloadedBytes = null,
    long? BytesPerSecond = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    string? Error = null);
