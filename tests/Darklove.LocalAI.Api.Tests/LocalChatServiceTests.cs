using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Tests;

public sealed class LocalChatServiceTests
{
    private readonly RuleBasedEmotionAnalysisService _ruleBasedService = new();

    [Fact]
    public async Task ReplyAsync_UsesLocalModel_ForNormalConversation()
    {
        var modelClient = new FakeModelClient("İyiyim, teşekkür ederim. Sen nasılsın? 😊");
        var service = CreateService(modelClient);

        var result = await service.ReplyAsync(new ChatRequest("naber nasıl gidiyor"));

        Assert.Equal("İyiyim, teşekkür ederim. Sen nasılsın?", result.AssistantMessage);
        Assert.Equal("open-source-model", result.AnalysisMethod);
        Assert.Equal("qwen3:4b", result.Model);
        Assert.False(result.NeedsSupportWarning);
        Assert.Equal(1, modelClient.ChatCallCount);
        Assert.Equal(0, modelClient.ClassifyCallCount);
    }

    [Fact]
    public async Task ReplyAsync_DoesNotCallModel_ForCrisisText()
    {
        var modelClient = new FakeModelClient("Bu cevap kullanılmamalı.");
        var service = CreateService(modelClient);

        var result = await service.ReplyAsync(new ChatRequest("Artık yaşamak istemiyorum."));

        Assert.Equal("high", result.RiskLevel);
        Assert.True(result.NeedsSupportWarning);
        Assert.Equal("rule-based-safety", result.AnalysisMethod);
        Assert.Contains("112", result.AssistantMessage);
        Assert.Equal(0, modelClient.ChatCallCount);
    }

    [Fact]
    public async Task ReplyAsync_ReturnsFriendlyMessage_WhenModelFails()
    {
        var modelClient = new FakeModelClient(
            new LocalModelException("model-unavailable", "Model kapalı."));
        var service = CreateService(modelClient);

        var result = await service.ReplyAsync(new ChatRequest("naber"));

        Assert.Equal("local-model-unavailable", result.AnalysisMethod);
        Assert.Equal("model-unavailable", result.FallbackReason);
        Assert.Contains("yerel model", result.AssistantMessage, StringComparison.OrdinalIgnoreCase);
    }

    private LocalChatService CreateService(IOpenSourceModelClient modelClient)
    {
        var options = Options.Create(new LocalModelOptions
        {
            Enabled = true,
            Provider = "ollama",
            Endpoint = "http://localhost:11434",
            Model = "qwen3:4b",
            TimeoutSeconds = 30
        });

        return new LocalChatService(
            _ruleBasedService,
            modelClient,
            new LocalModelSelection(options),
            options,
            NullLogger<LocalChatService>.Instance);
    }

    private sealed class FakeModelClient : IOpenSourceModelClient
    {
        private readonly string? _answer;
        private readonly Exception? _exception;

        public FakeModelClient(string answer)
        {
            _answer = answer;
        }

        public FakeModelClient(Exception exception)
        {
            _exception = exception;
        }

        public int ChatCallCount { get; private set; }

        public int ClassifyCallCount { get; private set; }

        public Task<string> ChatAsync(
            string userText,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            ChatCallCount++;

            return _exception is not null
                ? Task.FromException<string>(_exception)
                : Task.FromResult(_answer!);
        }

        public Task<OpenSourceModelClassification> ClassifyAsync(
            string userText,
            CancellationToken cancellationToken = default)
        {
            ClassifyCallCount++;

            return Task.FromResult(new OpenSourceModelClassification(
                "neutral",
                0.5,
                new Dictionary<string, double>()));
        }

        public Task<OpenSourceModelStatus> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new OpenSourceModelStatus("ollama", "qwen3:4b", true, true, "ready"));
        }
    }
}
