using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed class OllamaOpenSourceModelClient(
    HttpClient httpClient,
    ILocalModelSelection selection) : IOpenSourceModelClient
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

    private const string ChatSystemPrompt =
        """
        Sen Darklove Local AI içinde çalışan yerel Türkçe sohbet asistanısın.
        Kullanıcıyla doğal, kısa ve anlaşılır biçimde konuş.
        Her mesajı duygu analizi raporuna çevirmeye çalışma.
        Araştırma, yazılım, okul projesi ve genel bilgi sorularını doğrudan yanıtla.
        Tıbbi veya psikolojik teşhis koyma; acil risk durumunda profesyonel destek öner.
        Teknik etiket, model adı, confidence veya analiz yöntemi yazma.
        Emoji kullanma.
        """;

    public async Task<string> ChatAsync(
        string userText,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<OllamaMessage>
        {
            new("system", ChatSystemPrompt)
        };

        messages.AddRange(history.Select(message =>
            new OllamaMessage(message.Role, message.Content)));
        messages.Add(new OllamaMessage("user", userText));

        var request = new OllamaPlainChatRequest(
            selection.Model,
            messages,
            Stream: false,
            Think: false,
            Options: new OllamaRuntimeOptions(Temperature: 0.4, NumPredict: 700));

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(
                "/api/chat",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new LocalModelException(
                "model-timeout",
                "Yerel sohbet isteği zaman aşımına uğradı.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new LocalModelException(
                "model-unavailable",
                "Ollama çalışma zamanına ulaşılamadı.",
                exception);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new LocalModelException(
                "model-not-found",
                $"'{selection.Model}' modeli Ollama içinde bulunamadı.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new LocalModelException(
                "model-http-error",
                $"Ollama HTTP {(int)response.StatusCode} yanıtı döndürdü.");
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            JsonOptions,
            cancellationToken);
        var content = ollamaResponse?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new LocalModelException(
                "invalid-model-response",
                "Yerel sohbet modeli boş yanıt döndürdü.");
        }

        return content.Trim();
    }

    public async Task<OpenSourceModelClassification> ClassifyAsync(
        string userText,
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest(
            selection.Model,
            [
                new OllamaMessage("system", SystemPrompt),
                new OllamaMessage("user", userText)
            ],
            Stream: false,
            Think: false,
            Format: ResponseSchema,
            Options: new OllamaRuntimeOptions(Temperature: 0, NumPredict: 200));

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsJsonAsync(
                "/api/chat",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new LocalModelException(
                "model-timeout",
                "Yerel model isteği zaman aşımına uğradı.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new LocalModelException(
                "model-unavailable",
                "Ollama çalışma zamanına ulaşılamadı.",
                exception);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new LocalModelException(
                "model-not-found",
                $"'{selection.Model}' modeli Ollama içinde bulunamadı.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new LocalModelException(
                "model-http-error",
                $"Ollama HTTP {(int)response.StatusCode} yanıtı döndürdü.");
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            JsonOptions,
            cancellationToken);
        var content = ollamaResponse?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new LocalModelException(
                "invalid-model-response",
                "Yerel model boş yanıt döndürdü.");
        }

        ModelOutput? output;

        try
        {
            output = JsonSerializer.Deserialize<ModelOutput>(
                ExtractJsonObject(content),
                JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new LocalModelException(
                "invalid-model-response",
                "Yerel model geçerli JSON döndürmedi.",
                exception);
        }

        return ValidateOutput(output);
    }

    public async Task<OpenSourceModelStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/tags", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new OpenSourceModelStatus(
                    "ollama",
                    selection.Model,
                    RuntimeAvailable: false,
                    ModelAvailable: false,
                    Status: "runtime-error");
            }

            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(
                JsonOptions,
                cancellationToken);
            var modelAvailable = tags?.Models?.Any(model =>
                string.Equals(model.Name, selection.Model, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(model.Model, selection.Model, StringComparison.OrdinalIgnoreCase)) == true;

            return new OpenSourceModelStatus(
                "ollama",
                selection.Model,
                RuntimeAvailable: true,
                ModelAvailable: modelAvailable,
                Status: modelAvailable ? "ready" : "model-not-found");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            return new OpenSourceModelStatus(
                "ollama",
                selection.Model,
                RuntimeAvailable: false,
                ModelAvailable: false,
                Status: "runtime-unavailable");
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

    private static string ExtractJsonObject(string content)
    {
        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');

        return firstBrace >= 0 && lastBrace > firstBrace
            ? content[firstBrace..(lastBrace + 1)]
            : content;
    }

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaMessage> Messages,
        bool Stream,
        bool Think,
        JsonElement Format,
        OllamaRuntimeOptions Options);

    private sealed record OllamaPlainChatRequest(
        string Model,
        IReadOnlyList<OllamaMessage> Messages,
        bool Stream,
        bool Think,
        OllamaRuntimeOptions Options);

    private sealed record OllamaMessage(string Role, string Content);

    private sealed record OllamaRuntimeOptions(
        double Temperature,
        [property: JsonPropertyName("num_predict")] int NumPredict);

    private sealed record OllamaChatResponse(OllamaMessage? Message);

    private sealed record ModelOutput(
        string DetectedEmotion,
        double Confidence,
        Dictionary<string, double>? Scores);

    private sealed record OllamaTagsResponse(IReadOnlyList<OllamaModel>? Models);

    private sealed record OllamaModel(string Name, string Model);
}
