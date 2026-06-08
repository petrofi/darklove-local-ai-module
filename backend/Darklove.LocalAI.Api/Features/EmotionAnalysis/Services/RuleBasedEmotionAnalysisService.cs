using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed class RuleBasedEmotionAnalysisService : IEmotionAnalysisService
{
    private const string NoRisk = "none";
    private const string HighRisk = "high";

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<KeywordRule>> EmotionRules =
        new Dictionary<string, IReadOnlyList<KeywordRule>>(StringComparer.Ordinal)
        {
            ["sadness"] = CreateRules(
                "üzgün",
                "üzgünüm",
                "yalnız",
                "yalnızım",
                "yorgun",
                "yorgunum",
                "kırgınım",
                "boşluktayım",
                "ağlamak istiyorum",
                "mutsuzum",
                "moralim bozuk"),
            ["anxiety"] = CreateRules(
                "kaygı",
                "kaygılıyım",
                "stresliyim",
                "panik hissediyorum",
                "panik oldum",
                "korkuyorum",
                "endişeliyim",
                "gerginim"),
            ["hope"] = CreateRules(
                "umut",
                "umutluyum",
                "iyi hissediyorum",
                "başaracağım",
                "güçlüyüm",
                "devam edeceğim",
                "toparlanacağım"),
            ["anger"] = CreateRules(
                "sinirliyim",
                "öfkeliyim",
                "öfke hissediyorum",
                "bıktım",
                "kızgınım",
                "dayanamıyorum")
        };

    private static readonly IReadOnlyList<KeywordRule> CrisisRules = CreateRules(
        "kendime zarar",
        "kendime zarar vermek istiyorum",
        "yaşamak istemiyorum",
        "intihar",
        "intihar etmek istiyorum",
        "ölmek istiyorum",
        "canıma kıymak istiyorum");

    private static readonly IReadOnlyDictionary<string, string> MotivationMessages =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sadness"] = "Bugün zor geçiyor olabilir. Küçük bir mola verip güvendiğin biriyle konuşmak ve kendine nazik davranmak iyi gelebilir.",
            ["anxiety"] = "Şu an kaygılı hissetmen anlaşılır. Yavaşça nefes alıp çözebileceğin en küçük adıma odaklanmayı deneyebilirsin.",
            ["hope"] = "İçindeki umut değerli. Küçük ve gerçekçi adımlarla ilerlemeye devam edebilirsin.",
            ["anger"] = "Öfkeni fark etmen önemli. Tepki vermeden önce kısa bir ara vermek ve sakinleşmek için kendine alan açmak iyi gelebilir.",
            ["mixed"] = "Birden fazla duyguyu aynı anda yaşıyor olabilirsin. Duygularını tek tek adlandırıp şu an en çok neye ihtiyaç duyduğunu düşünmeyi deneyebilirsin.",
            ["neutral"] = "Belirgin bir duygu eşleşmesi bulunamadı. Nasıl hissettiğini biraz daha ayrıntılı anlatmayı deneyebilirsin."
        };

    private const string CrisisSupportMessage =
        "Yazdıkların acil desteğe ihtiyaç duyabileceğini gösteriyor. Lütfen şu anda yalnız kalma; güvendiğin bir kişiye hemen ulaş ve profesyonel destek iste. Kendine zarar verme tehlikesi varsa veya hayati bir acil durum yaşıyorsan Türkiye'de 112 Acil Çağrı Merkezi'ni ara.";

    public EmotionAnalysisResponse Analyze(string userText)
    {
        var normalizedText = Normalize(userText);
        var scores = new Dictionary<string, int>(StringComparer.Ordinal);
        var matchedKeywords =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var (emotion, rules) in EmotionRules)
        {
            var matches = rules
                .Where(rule => rule.Pattern.IsMatch(normalizedText))
                .Select(rule => rule.Keyword)
                .ToArray();

            scores[emotion] = matches.Length;

            if (matches.Length > 0)
            {
                matchedKeywords[emotion] = matches;
            }
        }

        var detectedEmotion = DetectEmotion(scores);
        var confidence = CalculateConfidence(detectedEmotion, scores);
        var hasCrisisSignal = CrisisRules.Any(rule => rule.Pattern.IsMatch(normalizedText));
        var riskLevel = hasCrisisSignal ? HighRisk : NoRisk;
        var message = hasCrisisSignal
            ? CrisisSupportMessage
            : MotivationMessages[detectedEmotion];

        return new EmotionAnalysisResponse(
            detectedEmotion,
            confidence,
            scores,
            matchedKeywords,
            riskLevel,
            hasCrisisSignal,
            message);
    }

    private static string DetectEmotion(IReadOnlyDictionary<string, int> scores)
    {
        var topScore = scores.Values.Max();

        if (topScore == 0)
        {
            return "neutral";
        }

        var topEmotionCount = scores.Values.Count(score => score == topScore);
        return topEmotionCount > 1
            ? "mixed"
            : scores.Single(pair => pair.Value == topScore).Key;
    }

    private static double CalculateConfidence(
        string detectedEmotion,
        IReadOnlyDictionary<string, int> scores)
    {
        if (detectedEmotion == "neutral")
        {
            return 0.0;
        }

        var orderedScores = scores.Values.OrderByDescending(score => score).ToArray();
        var topScore = orderedScores[0];

        if (detectedEmotion == "mixed")
        {
            return Math.Round(Math.Min(0.70, 0.40 + (topScore * 0.10)), 2);
        }

        var secondScore = orderedScores[1];
        var scoreMargin = topScore - secondScore;
        var confidence = 0.50 + (topScore * 0.10) + (scoreMargin * 0.10);

        return Math.Round(Math.Min(0.95, confidence), 2);
    }

    private static IReadOnlyList<KeywordRule> CreateRules(params string[] keywords)
    {
        return keywords
            .Select(keyword =>
            {
                var normalizedKeyword = Normalize(keyword);
                var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(normalizedKeyword)}(?![\p{{L}}\p{{N}}])";
                return new KeywordRule(
                    normalizedKeyword,
                    new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));
            })
            .ToArray();
    }

    private static string Normalize(string text)
    {
        var normalized = text
            .Normalize(NormalizationForm.FormC)
            .ToLower(TurkishCulture);

        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }

    private sealed record KeywordRule(string Keyword, Regex Pattern);
}
