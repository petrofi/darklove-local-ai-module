using System.Net;
using System.Text;
using System.Text.Json;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Tests;

public sealed class OllamaOpenSourceModelClientTests
{
    [Fact]
    public async Task ClassifyAsync_ParsesStructuredOllamaResponse()
    {
        string? capturedRequest = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            capturedRequest = await request.Content!.ReadAsStringAsync();

            return JsonResponse(
                """
                {
                  "message": {
                    "role": "assistant",
                    "content": "{\"detectedEmotion\":\"anxiety\",\"confidence\":0.82,\"scores\":{\"sadness\":0.05,\"anxiety\":0.82,\"hope\":0.02,\"anger\":0.01,\"neutral\":0.10}}"
                  }
                }
                """);
        });
        var client = CreateClient(handler);

        var result = await client.ClassifyAsync("Toplantı için çok endişeliyim.");

        Assert.Equal("anxiety", result.DetectedEmotion);
        Assert.Equal(0.82, result.Confidence);
        Assert.Equal(0.82, result.Scores["anxiety"]);
        Assert.NotNull(capturedRequest);

        using var requestDocument = JsonDocument.Parse(capturedRequest);
        Assert.Equal(
            "qwen3:4b",
            requestDocument.RootElement.GetProperty("model").GetString());
        Assert.False(requestDocument.RootElement.GetProperty("stream").GetBoolean());
        Assert.True(requestDocument.RootElement.TryGetProperty("format", out _));
    }

    [Fact]
    public async Task ClassifyAsync_RejectsInvalidModelContract()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(
            JsonResponse(
                """
                {
                  "message": {
                    "role": "assistant",
                    "content": "{\"detectedEmotion\":\"unknown\",\"confidence\":5,\"scores\":{}}"
                  }
                }
                """)));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<LocalModelException>(
            () => client.ClassifyAsync("Test"));

        Assert.Equal("invalid-model-response", exception.ReasonCode);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsReady_WhenConfiguredModelExists()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(
            JsonResponse(
                """
                {
                  "models": [
                    {
                      "name": "qwen3:4b",
                      "model": "qwen3:4b"
                    }
                  ]
                }
                """)));
        var client = CreateClient(handler);

        var result = await client.GetStatusAsync();

        Assert.True(result.RuntimeAvailable);
        Assert.True(result.ModelAvailable);
        Assert.Equal("ready", result.Status);
    }

    private static OllamaOpenSourceModelClient CreateClient(HttpMessageHandler handler)
    {
        return new OllamaOpenSourceModelClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:11434")
            },
            Options.Create(new LocalModelOptions
            {
                Enabled = true,
                Provider = "ollama",
                Endpoint = "http://localhost:11434",
                Model = "qwen3:4b",
                TimeoutSeconds = 30
            }));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }
}
