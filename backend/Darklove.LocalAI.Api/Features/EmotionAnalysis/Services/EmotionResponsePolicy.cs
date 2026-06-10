namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

internal static class EmotionResponsePolicy
{
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

    public const string CrisisSupportMessage =
        "Yazdıkların acil desteğe ihtiyaç duyabileceğini gösteriyor. Lütfen şu anda yalnız kalma; güvendiğin bir kişiye hemen ulaş ve profesyonel destek iste. Kendine zarar verme tehlikesi varsa veya hayati bir acil durum yaşıyorsan Türkiye'de 112 Acil Çağrı Merkezi'ni ara.";

    public static string GetMotivationMessage(string emotion)
    {
        return MotivationMessages.TryGetValue(emotion, out var message)
            ? message
            : MotivationMessages["neutral"];
    }
}
