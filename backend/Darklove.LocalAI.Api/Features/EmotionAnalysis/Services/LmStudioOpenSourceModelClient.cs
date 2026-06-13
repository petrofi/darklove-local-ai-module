using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed partial class LmStudioOpenSourceModelClient(
    HttpClient httpClient,
    ILocalModelSelection selection,
    ILocalModelRuntimeLauncher runtimeLauncher) :
    IOpenSourceModelClient,
    ILocalModelManager
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly string[] RequiredScoreNames =
        ["sadness", "anxiety", "hope", "anger", "neutral"];

    private static readonly HashSet<string> AllowedEmotions =
        new(
            ["sadness", "anxiety", "hope", "anger", "neutral", "mixed"],
            StringComparer.Ordinal);

    private static readonly object ScoreSchema = new
    {
        type = "number",
        minimum = 0,
        maximum = 1
    };

    private static readonly JsonElement ResponseSchema = JsonSerializer.SerializeToElement(
        new
        {
            type = "object",
            properties = new
            {
                detectedEmotion = new
                {
                    type = "string",
                    @enum = AllowedEmotions.OrderBy(value => value).ToArray()
                },
                confidence = new
                {
                    type = "number",
                    minimum = 0,
                    maximum = 1
                },
                scores = new
                {
                    type = "object",
                    properties = new
                    {
                        sadness = ScoreSchema,
                        anxiety = ScoreSchema,
                        hope = ScoreSchema,
                        anger = ScoreSchema,
                        neutral = ScoreSchema
                    },
                    required = RequiredScoreNames,
                    additionalProperties = false
                }
            },
            required = new[] { "detectedEmotion", "confidence", "scores" },
            additionalProperties = false
        });

    private const string SystemPrompt =
        """
        Sen yalnızca Türkçe duygu sınıflandırması yapan yerel bir analiz motorusun.
        Kullanıcı metnini sadness, anxiety, hope, anger, neutral veya mixed olarak sınıflandır.
        İki duygu benzer derecede baskınsa mixed kullan.
        confidence ve her kategori skoru 0 ile 1 arasında olmalıdır.
        Kullanıcıya tavsiye verme, metni tekrar etme ve açıklama ekleme.
        Yalnızca verilen JSON şemasına uyan JSON üret.
        """;

    public async Task<OpenSourceModelClassification> ClassifyAsync(
        string userText,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = selection.Model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userText }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "emotion_analysis",
                    strict = true,
                    schema = ResponseSchema
                }
            },
            temperature = 0,
            max_tokens = 200,
            stream = false
        };

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(
                "/v1/chat/completions",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new LocalModelException(
                "model-timeout",
                "LM Studio model isteği zaman aşımına uğradı.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new LocalModelException(
                "model-unavailable",
                "LM Studio çalışma zamanına ulaşılamadı.",
                exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            var reason = response.StatusCode == HttpStatusCode.NotFound
                ? "model-not-found"
                : "model-http-error";

            throw new LocalModelException(
                reason,
                $"LM Studio HTTP {(int)response.StatusCode} yanıtı döndürdü.");
        }

        var lmStudioResponse =
            await response.Content.ReadFromJsonAsync<LmStudioChatResponse>(
                JsonOptions,
                cancellationToken);
        var content = lmStudioResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new LocalModelException(
                "invalid-model-response",
                "LM Studio modeli boş yanıt döndürdü.");
        }

        ModelOutput? output;

        try
        {
            output = JsonSerializer.Deserialize<ModelOutput>(content, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new LocalModelException(
                "invalid-model-response",
                "LM Studio modeli geçerli JSON döndürmedi.",
                exception);
        }

        return ValidateOutput(output);
    }

    public async Task<OpenSourceModelStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);
        var selectedModel = catalog.Models.FirstOrDefault(model =>
            string.Equals(model.Key, selection.Model, StringComparison.OrdinalIgnoreCase));

        return new OpenSourceModelStatus(
            "lmstudio",
            selection.Model,
            catalog.RuntimeAvailable,
            selectedModel is not null,
            selectedModel is null
                ? catalog.Status
                : selectedModel.IsLoaded ? "ready" : "model-not-loaded");
    }

    public async Task<LocalModelCatalog> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await TryGetCatalogResponseAsync(cancellationToken);

        if (response is null && await runtimeLauncher.EnsureRunningAsync(cancellationToken))
        {
            for (var attempt = 0; attempt < 20 && response is null; attempt++)
            {
                await Task.Delay(250, cancellationToken);
                response = await TryGetCatalogResponseAsync(cancellationToken);
            }
        }

        if (response is null)
        {
            return new LocalModelCatalog(
                "lmstudio",
                selection.Model,
                RuntimeAvailable: false,
                Status: "runtime-unavailable",
                []);
        }

        var models = response.Models
            .Where(model => string.Equals(model.Type, "llm", StringComparison.OrdinalIgnoreCase))
            .Select(model => new LocalModelInfo(
                model.Key,
                model.DisplayName,
                "lmstudio",
                model.SizeBytes,
                model.ParamsString,
                model.Quantization?.Name,
                model.LoadedInstances.Count > 0,
                model.Capabilities?.Vision ?? false))
            .OrderBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selected = models.FirstOrDefault(model =>
            string.Equals(model.Key, selection.Model, StringComparison.OrdinalIgnoreCase));

        return new LocalModelCatalog(
            "lmstudio",
            selection.Model,
            RuntimeAvailable: true,
            Status: selected is null
                ? "model-not-found"
                : selected.IsLoaded ? "ready" : "model-not-loaded",
            models);
    }

    public async Task<LocalModelSelectionResult> SelectAsync(
        string model,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);
        var selectedModel = catalog.Models.FirstOrDefault(item =>
            string.Equals(item.Key, model, StringComparison.OrdinalIgnoreCase));

        if (selectedModel is null)
        {
            throw new LocalModelManagementException(
                "model-not-found",
                "Seçilen model LM Studio kataloğunda bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        if (!selectedModel.IsLoaded)
        {
            using var response = await httpClient.PostAsJsonAsync(
                "/api/v1/models/load",
                new
                {
                    model = selectedModel.Key,
                    context_length = 4096,
                    flash_attention = true
                },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await ReadErrorAsync(response, cancellationToken);
                throw new LocalModelManagementException(
                    "model-load-failed",
                    detail ?? "Model LM Studio belleğine yüklenemedi.",
                    StatusCodes.Status502BadGateway);
            }
        }

        selection.Select(selectedModel.Key);
        return new LocalModelSelectionResult(
            "lmstudio",
            selectedModel.Key,
            Loaded: true,
            "selected");
    }

    public async Task<LocalModelDownloadStatus> StartDownloadAsync(
        string model,
        string? quantization,
        CancellationToken cancellationToken = default)
    {
        ValidateDownloadInput(model, quantization);

        using var response = await httpClient.PostAsJsonAsync(
            "/api/v1/models/download",
            new
            {
                model,
                quantization = string.IsNullOrWhiteSpace(quantization)
                    ? null
                    : quantization
            },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadErrorAsync(response, cancellationToken);
            throw new LocalModelManagementException(
                "download-failed",
                detail ?? "LM Studio model indirme isteğini kabul etmedi.",
                StatusCodes.Status502BadGateway);
        }

        var status = await response.Content.ReadFromJsonAsync<LmStudioDownloadStatus>(
            JsonOptions,
            cancellationToken);

        return status?.ToPublicStatus()
            ?? throw new LocalModelManagementException(
                "invalid-download-response",
                "LM Studio geçerli bir indirme işi döndürmedi.",
                StatusCodes.Status502BadGateway);
    }

    public async Task<LocalModelDownloadStatus> GetDownloadStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (!DownloadJobIdRegex().IsMatch(jobId))
        {
            throw new LocalModelManagementException(
                "invalid-job-id",
                "İndirme işi kimliği geçersiz.");
        }

        using var response = await httpClient.GetAsync(
            $"/api/v1/models/download/status/{Uri.EscapeDataString(jobId)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new LocalModelManagementException(
                "download-not-found",
                "İndirme işi bulunamadı.",
                StatusCodes.Status404NotFound);
        }

        var status = await response.Content.ReadFromJsonAsync<LmStudioDownloadStatus>(
            JsonOptions,
            cancellationToken);

        return status?.ToPublicStatus()
            ?? throw new LocalModelManagementException(
                "invalid-download-response",
                "LM Studio geçerli bir indirme durumu döndürmedi.",
                StatusCodes.Status502BadGateway);
    }

    private async Task<LmStudioModelsResponse?> TryGetCatalogResponseAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<LmStudioModelsResponse>(
                "/api/v1/models",
                JsonOptions,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private static void ValidateDownloadInput(string model, string? quantization)
    {
        var validCatalogId =
            !model.Contains("://", StringComparison.Ordinal) &&
            ModelIdRegex().IsMatch(model);
        var validHuggingFaceUrl =
            Uri.TryCreate(model, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            string.Equals(uri.Host, "huggingface.co", StringComparison.OrdinalIgnoreCase);

        if (!validCatalogId && !validHuggingFaceUrl)
        {
            throw new LocalModelManagementException(
                "invalid-model-id",
                "Model adı katalog kimliği veya huggingface.co bağlantısı olmalıdır.");
        }

        if (!string.IsNullOrWhiteSpace(quantization) &&
            !QuantizationRegex().IsMatch(quantization))
        {
            throw new LocalModelManagementException(
                "invalid-quantization",
                "Quantization değeri geçersiz.");
        }
    }

    private static async Task<string?> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<LmStudioError>(
                JsonOptions,
                cancellationToken);
            return problem?.Error?.Message ?? problem?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static OpenSourceModelClassification ValidateOutput(ModelOutput? output)
    {
        if (output is null ||
            !AllowedEmotions.Contains(output.DetectedEmotion) ||
            output.Confidence is < 0 or > 1 ||
            output.Scores is null)
        {
            throw new LocalModelException(
                "invalid-model-response",
                "Yerel model yanıtı beklenen sınıflandırma sözleşmesine uymuyor.");
        }

        var scores = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var scoreName in RequiredScoreNames)
        {
            if (!output.Scores.TryGetValue(scoreName, out var score) ||
                score is < 0 or > 1)
            {
                throw new LocalModelException(
                    "invalid-model-response",
                    $"Yerel model yanıtında geçerli '{scoreName}' skoru bulunamadı.");
            }

            scores[scoreName] = Math.Round(score, 3);
        }

        return new OpenSourceModelClassification(
            output.DetectedEmotion,
            Math.Round(output.Confidence, 3),
            scores);
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._/@:-]{0,199}$")]
    private static partial Regex ModelIdRegex();

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,49}$")]
    private static partial Regex QuantizationRegex();

    [GeneratedRegex(@"^job_[A-Za-z0-9]+$")]
    private static partial Regex DownloadJobIdRegex();

    private sealed record LmStudioChatResponse(
        IReadOnlyList<LmStudioChoice>? Choices);

    private sealed record LmStudioChoice(LmStudioMessage? Message);

    private sealed record LmStudioMessage(string? Content);

    private sealed record ModelOutput(
        string DetectedEmotion,
        double Confidence,
        Dictionary<string, double>? Scores);

    private sealed record LmStudioModelsResponse(
        IReadOnlyList<LmStudioModel> Models);

    private sealed record LmStudioModel(
        string Type,
        string Key,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("size_bytes")] long SizeBytes,
        [property: JsonPropertyName("params_string")] string? ParamsString,
        LmStudioQuantization? Quantization,
        [property: JsonPropertyName("loaded_instances")]
        IReadOnlyList<LmStudioLoadedInstance> LoadedInstances,
        LmStudioCapabilities? Capabilities);

    private sealed record LmStudioQuantization(string? Name);

    private sealed record LmStudioLoadedInstance(string Id);

    private sealed record LmStudioCapabilities(bool Vision);

    private sealed record LmStudioError(
        string? Message,
        LmStudioErrorDetail? Error);

    private sealed record LmStudioErrorDetail(string? Message);

    private sealed record LmStudioDownloadStatus(
        [property: JsonPropertyName("job_id")] string? JobId,
        string Status,
        [property: JsonPropertyName("total_size_bytes")] long? TotalSizeBytes,
        [property: JsonPropertyName("downloaded_bytes")] long? DownloadedBytes,
        [property: JsonPropertyName("bytes_per_second")] long? BytesPerSecond,
        [property: JsonPropertyName("started_at")] DateTimeOffset? StartedAt,
        [property: JsonPropertyName("completed_at")] DateTimeOffset? CompletedAt,
        string? Error)
    {
        public LocalModelDownloadStatus ToPublicStatus()
        {
            return new LocalModelDownloadStatus(
                JobId,
                Status,
                TotalSizeBytes,
                DownloadedBytes,
                BytesPerSecond,
                StartedAt,
                CompletedAt,
                Error);
        }
    }
}
