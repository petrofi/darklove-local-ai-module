using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Tests;

public sealed class HybridEmotionAnalysisServiceTests
{
    private readonly RuleBasedEmotionAnalysisService _ruleBasedService = new();

    [Fact]
    public async Task AnalyzeAsync_UsesOpenSourceModel_WhenModelSucceeds()
    {
        var modelScores = new Dictionary<string, double>
        {
            ["sadness"] = 0.86,
            ["anxiety"] = 0.08,
            ["hope"] = 0.02,
            ["anger"] = 0.01,
            ["neutral"] = 0.03
        };
        var modelClient = new FakeModelClient(
            new OpenSourceModelClassification("sadness", 0.86, modelScores));
        var service = CreateService(modelClient);

        var result = await service.AnalyzeAsync("İçimde ağır bir hüzün var.");

        Assert.Equal("sadness", result.DetectedEmotion);
        Assert.Equal(0.86, result.Confidence);
        Assert.Equal("open-source-model", result.AnalysisMethod);
        Assert.Equal("qwen3:4b", result.Model);
        Assert.Equal(0.86, result.ModelScores?["sadness"]);
        Assert.Null(result.FallbackReason);
        Assert.Equal(1, modelClient.ClassifyCallCount);
    }

    [Fact]
    public async Task AnalyzeAsync_FallsBackToRules_WhenModelFails()
    {
        var modelClient = new FakeModelClient(
            new LocalModelException("model-unavailable", "Ollama kapalı."));
        var service = CreateService(modelClient);

        var result = await service.AnalyzeAsync("Bugün yalnızım.");

        Assert.Equal("sadness", result.DetectedEmotion);
        Assert.Equal("rule-based-fallback", result.AnalysisMethod);
        Assert.Equal("model-unavailable", result.FallbackReason);
        Assert.Equal("qwen3:4b", result.Model);
    }

    [Fact]
    public async Task AnalyzeAsync_DoesNotCallModel_ForCrisisText()
    {
        var modelClient = new FakeModelClient(
            new OpenSourceModelClassification(
                "neutral",
                0.9,
                new Dictionary<string, double>()));
        var service = CreateService(modelClient);

        var result = await service.AnalyzeAsync("Artık yaşamak istemiyorum.");

        Assert.Equal("high", result.RiskLevel);
        Assert.Equal("rule-based-safety", result.AnalysisMethod);
        Assert.Contains("112", result.MotivationMessage);
        Assert.Equal(0, modelClient.ClassifyCallCount);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesRules_WhenModelIsDisabled()
    {
        var modelClient = new FakeModelClient(
            new OpenSourceModelClassification(
                "anger",
                0.9,
                new Dictionary<string, double>()));
        var service = CreateService(modelClient, enabled: false);

        var result = await service.AnalyzeAsync("Bugün yalnızım.");

        Assert.Equal("sadness", result.DetectedEmotion);
        Assert.Equal("rule-based", result.AnalysisMethod);
        Assert.Equal(0, modelClient.ClassifyCallCount);
    }

    private HybridEmotionAnalysisService CreateService(
        IOpenSourceModelClient modelClient,
        bool enabled = true)
    {
        var options = Options.Create(new LocalModelOptions
        {
            Enabled = enabled,
            Provider = "ollama",
            Endpoint = "http://localhost:11434",
            Model = "qwen3:4b",
            TimeoutSeconds = 30
        });

        return new HybridEmotionAnalysisService(
            _ruleBasedService,
            modelClient,
            new LocalModelSelection(options),
            options,
            NullLogger<HybridEmotionAnalysisService>.Instance);
    }

    private sealed class FakeModelClient : IOpenSourceModelClient
    {
        private readonly OpenSourceModelClassification? _classification;
        private readonly Exception? _exception;

        public FakeModelClient(OpenSourceModelClassification classification)
        {
            _classification = classification;
        }

        public FakeModelClient(Exception exception)
        {
            _exception = exception;
        }

        public int ClassifyCallCount { get; private set; }

        public Task<string> ChatAsync(
            string userText,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Sohbet yanıtı.");
        }

        public Task<OpenSourceModelClassification> ClassifyAsync(
            string userText,
            CancellationToken cancellationToken = default)
        {
            ClassifyCallCount++;

            return _exception is not null
                ? Task.FromException<OpenSourceModelClassification>(_exception)
                : Task.FromResult(_classification!);
        }

        public Task<OpenSourceModelStatus> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new OpenSourceModelStatus("ollama", "qwen3:4b", true, true, "ready"));
        }
    }
}
