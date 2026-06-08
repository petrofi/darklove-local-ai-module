using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

namespace Darklove.LocalAI.Api.Tests;

public sealed class RuleBasedEmotionAnalysisServiceTests
{
    private readonly RuleBasedEmotionAnalysisService _service = new();

    [Theory]
    [InlineData("Bugün çok üzgünüm.", "sadness")]
    [InlineData("Son günlerde kaygılıyım.", "anxiety")]
    [InlineData("Kendimi güçlü hissediyorum ve başaracağım.", "hope")]
    [InlineData("Bu duruma çok kızgınım.", "anger")]
    public void Analyze_DetectsSupportedEmotions(string text, string expectedEmotion)
    {
        var result = _service.Analyze(text);

        Assert.Equal(expectedEmotion, result.DetectedEmotion);
        Assert.True(result.Scores[expectedEmotion] > 0);
    }

    [Fact]
    public void Analyze_ReturnsNeutral_WhenNoRuleMatches()
    {
        var result = _service.Analyze("Bugün kitap okudum ve kahve içtim.");

        Assert.Equal("neutral", result.DetectedEmotion);
        Assert.Equal(0.0, result.Confidence);
        Assert.Empty(result.MatchedKeywords);
    }

    [Fact]
    public void Analyze_ReturnsMixed_WhenTopScoresAreEqual()
    {
        var result = _service.Analyze("Hem yalnızım hem de sinirliyim.");

        Assert.Equal("mixed", result.DetectedEmotion);
        Assert.Equal(1, result.Scores["sadness"]);
        Assert.Equal(1, result.Scores["anger"]);
        Assert.Equal(0.5, result.Confidence);
    }

    [Fact]
    public void Analyze_HandlesTurkishUppercaseCharacters()
    {
        var result = _service.Analyze("BUGÜN ÇOK YORGUNUM VE ÜZGÜNÜM.");

        Assert.Equal("sadness", result.DetectedEmotion);
        Assert.Contains("yorgunum", result.MatchedKeywords["sadness"]);
        Assert.Contains("üzgünüm", result.MatchedKeywords["sadness"]);
    }

    [Fact]
    public void Analyze_CountsRepeatedRuleOnlyOnce()
    {
        var result = _service.Analyze("Yorgunum, gerçekten yorgunum ve yine yorgunum.");

        Assert.Equal(1, result.Scores["sadness"]);
        Assert.Equal(0.7, result.Confidence);
    }

    [Fact]
    public void Analyze_UsesWholeWords_InsteadOfSubstrings()
    {
        var result = _service.Analyze("Sinir sistemi hakkında bir makale okuyorum.");

        Assert.Equal("neutral", result.DetectedEmotion);
        Assert.Equal(0, result.Scores["anger"]);
    }

    [Fact]
    public void Analyze_CalculatesDocumentedConfidenceFormula()
    {
        var result = _service.Analyze("Bugün yalnızım ve yorgunum.");

        Assert.Equal("sadness", result.DetectedEmotion);
        Assert.Equal(2, result.Scores["sadness"]);
        Assert.Equal(0.9, result.Confidence);
    }

    [Fact]
    public void Analyze_ReturnsSafeCrisisResponse_WhenRiskPhraseMatches()
    {
        var result = _service.Analyze("Artık yaşamak istemiyorum.");

        Assert.Equal("high", result.RiskLevel);
        Assert.True(result.NeedsSupportWarning);
        Assert.Contains("güvendiğin bir kişiye", result.MotivationMessage);
        Assert.Contains("profesyonel destek", result.MotivationMessage);
        Assert.Contains("112", result.MotivationMessage);
    }
}
