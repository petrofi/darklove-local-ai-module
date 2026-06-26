using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed partial class LocalChatService(
    IRuleBasedEmotionAnalysisService ruleBasedService,
    IOpenSourceModelClient modelClient,
    ILocalModelSelection selection,
    IOptions<LocalModelOptions> options,
    ILogger<LocalChatService> logger) : IChatService
{
    private readonly LocalModelOptions _options = options.Value;

    public async Task<ChatResponse> ReplyAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var userText = request.UserText?.Trim() ?? string.Empty;
        var safetyResult = ruleBasedService.Analyze(userText);

        if (safetyResult.NeedsSupportWarning)
        {
            return new ChatResponse(
                EmotionResponsePolicy.CrisisSupportMessage,
                safetyResult.RiskLevel,
                NeedsSupportWarning: true,
                AnalysisMethod: "rule-based-safety");
        }

        if (!_options.Enabled)
        {
            return new ChatResponse(
                "Şu anda yerel sohbet modeli kapalı görünüyor. Modeli açtıktan sonra tekrar yazabilirsin.",
                "none",
                NeedsSupportWarning: false,
                AnalysisMethod: "local-model-disabled",
                FallbackReason: "model-disabled");
        }

        try
        {
            var answer = await modelClient.ChatAsync(
                BuildModelPrompt(userText, request.HeartContext),
                SanitizeHistory(request.History),
                cancellationToken);

            return new ChatResponse(
                CleanAssistantMessage(answer),
                "none",
                NeedsSupportWarning: false,
                AnalysisMethod: "open-source-model",
                Model: selection.Model);
        }
        catch (LocalModelException exception)
        {
            logger.LogWarning(
                exception,
                "Yerel sohbet modeli kullanılamadı. ReasonCode: {ReasonCode}",
                exception.ReasonCode);

            return new ChatResponse(
                "Şu anda yerel model yanıt veremiyor. Model sunucusu hazır olduğunda tekrar deneyebilirsin.",
                "none",
                NeedsSupportWarning: false,
                AnalysisMethod: "local-model-unavailable",
                Model: selection.Model,
                FallbackReason: exception.ReasonCode);
        }
    }

    private static string BuildModelPrompt(string userText, HeartContext? heartContext)
    {
        var heartSummary = FormatHeartContext(heartContext);

        if (string.IsNullOrEmpty(heartSummary))
        {
            return userText;
        }

        return
            "Aşağıdaki kalp ritmi bilgisi, kullanıcının yerel AD8232/Arduino " +
            "modülünden gelen yaklaşık sohbet bağlamıdır; tıbbi teşhis değildir. " +
            "Kesin tanı koyma. Kullanıcı göğüs ağrısı, bayılma, nefes darlığı veya " +
            "hayati risk anlatırsa profesyonel destek ve 112 yönlendirmesi yap.\n\n" +
            $"Kalp ritmi özeti: {heartSummary}\n\n" +
            $"Kullanıcı mesajı: {userText}";
    }

    private static string FormatHeartContext(HeartContext? heartContext)
    {
        if (heartContext is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (heartContext.Bpm is >= 30 and <= 220)
        {
            parts.Add($"yaklaşık nabız {heartContext.Bpm} BPM");
        }

        var rhythm = NormalizeShortText(heartContext.Rhythm, 40);
        if (!string.IsNullOrEmpty(rhythm))
        {
            parts.Add($"ritim durumu {rhythm}");
        }

        var signalQuality = NormalizeShortText(heartContext.SignalQuality, 40);
        if (!string.IsNullOrEmpty(signalQuality))
        {
            parts.Add($"sinyal kalitesi {signalQuality}");
        }

        if (heartContext.LeadOff is true)
        {
            parts.Add("elektrot teması yok veya zayıf");
        }

        if (heartContext.SampleCount is > 0 and <= 20000)
        {
            parts.Add($"{heartContext.SampleCount} örnek okundu");
        }

        if (heartContext.AverageValue is >= 0 and <= 5000)
        {
            parts.Add($"ortalama ham değer {Math.Round(heartContext.AverageValue.Value, 1)}");
        }

        return string.Join("; ", parts);
    }

    private static string NormalizeShortText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = RepeatedSpacesRegex()
            .Replace(value.Trim(), " ");

        return cleaned.Length <= maxLength
            ? cleaned
            : cleaned[..maxLength];
    }

    private static IReadOnlyList<ChatMessage> SanitizeHistory(
        IReadOnlyList<ChatMessage>? history)
    {
        if (history is null || history.Count == 0)
        {
            return [];
        }

        return history
            .Where(message =>
                (string.Equals(message.Role, "user", StringComparison.Ordinal) ||
                 string.Equals(message.Role, "assistant", StringComparison.Ordinal)) &&
                !string.IsNullOrWhiteSpace(message.Content))
            .TakeLast(12)
            .Select(message => new ChatMessage(
                message.Role,
                message.Content.Length > 2000
                    ? message.Content[..2000]
                    : message.Content))
            .ToArray();
    }

    private static string CleanAssistantMessage(string answer)
    {
        var withoutEmoji = EmojiRegex().Replace(answer, string.Empty);

        return RepeatedSpacesRegex()
            .Replace(withoutEmoji, " ")
            .Replace(" \n", "\n", StringComparison.Ordinal)
            .Trim();
    }

    [GeneratedRegex(@"[\u2600-\u27BF]|[\uD800-\uDBFF][\uDC00-\uDFFF]")]
    private static partial Regex EmojiRegex();

    [GeneratedRegex(@"[^\S\r\n]{2,}")]
    private static partial Regex RepeatedSpacesRegex();
}
