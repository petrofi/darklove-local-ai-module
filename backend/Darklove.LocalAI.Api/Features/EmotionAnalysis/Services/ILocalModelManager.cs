using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface ILocalModelManager
{
    Task<LocalModelCatalog> GetCatalogAsync(
        CancellationToken cancellationToken = default);

    Task<LocalModelSelectionResult> SelectAsync(
        string model,
        CancellationToken cancellationToken = default);

    Task<LocalModelDownloadStatus> StartDownloadAsync(
        string model,
        string? quantization,
        CancellationToken cancellationToken = default);

    Task<LocalModelDownloadStatus> GetDownloadStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
