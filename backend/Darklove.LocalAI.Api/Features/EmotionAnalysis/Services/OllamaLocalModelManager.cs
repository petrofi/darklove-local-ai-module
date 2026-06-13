using System.Net.Http.Json;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed class OllamaLocalModelManager(
    HttpClient httpClient,
    ILocalModelSelection selection) : ILocalModelManager
{
    public async Task<LocalModelCatalog> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<OllamaTagsResponse>(
                "/api/tags",
                cancellationToken);
            var models = response?.Models?
                .Select(model => new LocalModelInfo(
                    model.Name,
                    model.Name,
                    "ollama",
                    model.Size,
                    model.Details?.ParameterSize,
                    model.Details?.QuantizationLevel,
                    IsLoaded: false,
                    SupportsVision: false))
                .OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];

            return new LocalModelCatalog(
                "ollama",
                selection.Model,
                RuntimeAvailable: true,
                Status: models.Any(model =>
                    string.Equals(model.Key, selection.Model, StringComparison.OrdinalIgnoreCase))
                        ? "ready"
                        : "model-not-found",
                models);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            return new LocalModelCatalog(
                "ollama",
                selection.Model,
                RuntimeAvailable: false,
                Status: "runtime-unavailable",
                []);
        }
    }

    public async Task<LocalModelSelectionResult> SelectAsync(
        string model,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);
        var exists = catalog.Models.Any(item =>
            string.Equals(item.Key, model, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            throw new LocalModelManagementException(
                "model-not-found",
                "Seçilen model Ollama içinde bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        selection.Select(model);
        return new LocalModelSelectionResult("ollama", model, Loaded: true, "selected");
    }

    public async Task<LocalModelDownloadStatus> StartDownloadAsync(
        string model,
        string? quantization,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/pull",
            new { model, stream = false },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new LocalModelManagementException(
                "download-failed",
                $"Ollama model indirme isteğini kabul etmedi (HTTP {(int)response.StatusCode}).",
                StatusCodes.Status502BadGateway);
        }

        selection.Select(model);
        return new LocalModelDownloadStatus(
            JobId: null,
            Status: "completed",
            CompletedAt: DateTimeOffset.UtcNow);
    }

    public Task<LocalModelDownloadStatus> GetDownloadStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        throw new LocalModelManagementException(
            "download-status-not-supported",
            "Ollama indirmeleri tek istek içinde tamamlanır.",
            StatusCodes.Status404NotFound);
    }

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModel>? Models);

    private sealed record OllamaModel(
        string Name,
        long Size,
        OllamaModelDetails? Details);

    private sealed record OllamaModelDetails(
        [property: System.Text.Json.Serialization.JsonPropertyName("parameter_size")]
        string? ParameterSize,
        [property: System.Text.Json.Serialization.JsonPropertyName("quantization_level")]
        string? QuantizationLevel);
}
